using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DocuFlow.Functions;

public class ProcessDocumentFunction
{
    private readonly ILogger<ProcessDocumentFunction> _logger;
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;
    private readonly DocumentIntelligenceClient _aiClient;
    private readonly AzureOpenAIClient? _openAiClient;
    private readonly string _mapModel;
    private readonly string _reduceModel;
    private readonly string? _ollamaEndpoint;

    public ProcessDocumentFunction(
        ILogger<ProcessDocumentFunction> logger,
        IDocumentRepository repository,
        IStorageService storageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _repository = repository;
        _storageService = storageService;

        QuestPDF.Settings.License = LicenseType.Community;

        var endpoint = configuration["AI_Service_Endpoint"];
        var key = configuration["AI_Service_Key"];
        _aiClient = new DocumentIntelligenceClient(new Uri(endpoint!), new AzureKeyCredential(key!));

        var openAiEndpoint = configuration["OpenAI_Endpoint"];
        var openAiKey = configuration["OpenAI_Key"];
        
        // Hybrid Model Strategy: Use Mini for chunks (cheap), Big for final synthesis (quality)
        _mapModel = configuration["OpenAI_Map_Model_Name"] ?? "gpt-4o-mini";
        _reduceModel = configuration["OpenAI_Reduce_Model_Name"] ?? "gpt-4o";
        _ollamaEndpoint = configuration["Ollama_Endpoint"];

        if (!string.IsNullOrEmpty(openAiEndpoint) && !string.IsNullOrEmpty(openAiKey))
        {
            _openAiClient = new AzureOpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));
        }
    }

    [Function(nameof(ProcessDocumentFunction))]
    public async Task Run(
        [ServiceBusTrigger("docuflow", Connection = "ServiceBusConnection")]
        string messageBody,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[DocuFlow] Processing message: {message}", messageBody);

        var payload = JsonSerializer.Deserialize<DocumentProcessingMessage>(messageBody);
        if (payload == null) return;

        var document = await _repository.GetByIdAsync(payload.Id, payload.TenantId, cancellationToken);
        if (document == null) return;

        if (document.Status == DocumentStatus.Processed)
        {
            _logger.LogInformation("[DocuFlow] Document {Id} is already processed. Skipping duplicate message.", document.Id);
            return;
        }

        if (document.Status == DocumentStatus.Processing)
        {
            _logger.LogWarning("[DocuFlow] Document {Id} is already in 'Processing' state. This might be a duplicate pickup.", document.Id);
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await _repository.UpdateAsync(document, cancellationToken);

            string? extractedText = null;
            var fields = new Dictionary<string, object?>();

            // Check if we already have extracted text in blob storage
            string extractedBlobName = $"extracted/{document.TenantId}/{document.Id}_content.txt";
            if (!string.IsNullOrEmpty(document.ExtractedTextUri))
            {
                _logger.LogInformation("[DocuFlow] Re-using existing text for {Id}...", document.Id);
                try { extractedText = await _storageService.GetContentAsync(document.ExtractedTextUri, cancellationToken); }
                catch { _logger.LogWarning("[DocuFlow] Failed to load existing text."); }
            }

            bool isPdf = document.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            bool needsStructuredData = payload.Category == DocumentCategory.Invoice || 
                                       payload.Category == DocumentCategory.Receipt || 
                                       payload.Category == DocumentCategory.Identity;

            if (string.IsNullOrEmpty(extractedText) && isPdf && !needsStructuredData)
            {
                _logger.LogInformation("[PdfPig] Local extraction for {Id}...", document.Id);
                try
                {
                    using var blobStream = await _storageService.GetBlobStreamAsync(document.BlobUri, cancellationToken);
                    using var seekableStream = new MemoryStream();
                    await blobStream.CopyToAsync(seekableStream, cancellationToken);
                    seekableStream.Position = 0;

                    using var pdf = PdfDocument.Open(seekableStream);
                    var sb = new StringBuilder();
                    foreach (var page in pdf.GetPages()) sb.AppendLine(page.Text);
                    extractedText = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(extractedText)) document.DocumentType = "Digital PDF";
                }
                catch (Exception ex) { _logger.LogWarning("[PdfPig] Failed: {Msg}", ex.Message); }
            }

            if (string.IsNullOrEmpty(extractedText))
            {
                string modelId = payload.Category switch
                {
                    DocumentCategory.Invoice => "prebuilt-invoice",
                    DocumentCategory.Receipt => "prebuilt-receipt",
                    DocumentCategory.Identity => "prebuilt-idDocument",
                    _ => "prebuilt-read"
                };

                var readUri = await _storageService.GenerateReadSasUriAsync(document.BlobUri, cancellationToken);
                var aiContent = new AnalyzeDocumentContent { UrlSource = new Uri(readUri) };
                var operation = await _aiClient.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, aiContent, cancellationToken: cancellationToken);
                var result = operation.Value;
                extractedText = result.Content;
                
                if (result.Documents.Count > 0)
                {
                    var doc = result.Documents[0];
                    document.DocumentType = doc.DocType;
                    document.ConfidenceScore = doc.Confidence;
                    foreach (var field in doc.Fields) fields.Add(field.Key, field.Value.Content);
                }
            }

            if (!string.IsNullOrEmpty(extractedText))
            {
                if (string.IsNullOrEmpty(document.ExtractedTextUri))
                {
                    await _storageService.UploadContentAsync(extractedBlobName, extractedText, "text/plain", cancellationToken);
                    document.ExtractedTextUri = extractedBlobName;
                }
                
                fields.Add("FullContentPreview", extractedText.Length > 2000 ? extractedText.Substring(0, 2000) + "..." : extractedText);

                if (payload.Category == DocumentCategory.Summary && (_openAiClient != null || !string.IsNullOrEmpty(_ollamaEndpoint)))
                {
                    document.Summary = await GenerateMapReduceSummaryAsync(extractedText, cancellationToken);
                    
                    if (document.Summary != null)
                    {
                        byte[] pdfBytes = GenerateSummaryPdf(document);
                        string summaryPdfName = $"summaries/{document.TenantId}/{document.Id}_summary.pdf";
                        await _storageService.UploadBytesAsync(summaryPdfName, pdfBytes, "application/pdf", cancellationToken);
                        document.SummaryPdfUri = summaryPdfName;
                    }
                }

                document.ExtractedData = fields;
                document.Status = DocumentStatus.Processed;
                _logger.LogInformation("[DocuFlow] Processed {Id}.", document.Id);
            }
            else document.Status = DocumentStatus.Failed;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[DocuFlow] Canceled for {Id}.", document.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DocuFlow] Error for {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
        }

        await _repository.UpdateAsync(document, CancellationToken.None);
    }

    private byte[] GenerateSummaryPdf(DocuFlow.Domain.Entities.Document doc)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("DocuFlow AI Insights").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Report for: {doc.FileName}").FontSize(12).Italic();
                    });
                });
                page.Content().PaddingVertical(20).Column(x =>
                {
                    x.Spacing(15);
                    x.Item().Text("Executive Summary").FontSize(18).SemiBold().FontColor(Colors.Blue.Medium);
                    x.Item().LineHorizontal(1);
                    x.Item().Text(doc.Summary).LineHeight(1.5f);
                });
                page.Footer().AlignCenter().Text(x => { x.Span("Page "); x.CurrentPageNumber(); });
            });
        }).GeneratePdf();
    }

    private async Task<string?> GenerateMapReduceSummaryAsync(string fullText, CancellationToken ct)
    {
        fullText = CleanDocumentText(fullText);
        
        _logger.LogInformation("[DocuFlow] Starting Optimized Map-Reduce ({Length} chars, Map: {Map}, Reduce: {Reduce})", 
            fullText.Length, _mapModel, _reduceModel);
        
        const int chunkSize = 50000;
        var mapTasks = new List<Task<string>>();
        using var semaphore = new SemaphoreSlim(10); // Increased to 10 for 250k TPM Global Standard models

        try
        {
            for (int i = 0; i < fullText.Length; i += chunkSize)
            {
                int currentChunkIndex = (i / chunkSize) + 1;
                int totalChunks = (int)Math.Ceiling((double)fullText.Length / chunkSize);
                var chunk = fullText.Substring(i, Math.Min(chunkSize, fullText.Length - i));
                
                mapTasks.Add(Task.Run(async () => 
                {
                    int retryCount = 0;
                    while (true)
                    {
                        await semaphore.WaitAsync(ct);
                        try
                        {
                            _logger.LogInformation("[OpenAI] Mapping chunk {Current}/{Total}...", currentChunkIndex, totalChunks);
                            
                            if (!string.IsNullOrEmpty(_ollamaEndpoint) && _openAiClient == null)
                                return "Ollama offline summary placeholder";

                            var client = _openAiClient!.GetChatClient(_mapModel);
                            ChatCompletion completion = await client.CompleteChatAsync(new ChatMessage[]
                            {
                                new SystemChatMessage("Summarize this document section concisely."),
                                new UserChatMessage(chunk)
                            }, cancellationToken: ct);
                            return completion.Content[0].Text;
                        }
                        catch (ClientResultException ex) when (ex.Message.Contains("429"))
                        {
                            if (++retryCount > 3) throw;
                            await Task.Delay((int)Math.Pow(2, retryCount) * 2000, ct);
                        }
                        finally { semaphore.Release(); }
                    }
                }));
            }

            var intermediateSummaries = await Task.WhenAll(mapTasks);
            var combinedSummaries = string.Join("\n\n---\n\n", intermediateSummaries);
            if (combinedSummaries.Length > 120000) combinedSummaries = combinedSummaries.Substring(0, 120000);

            _logger.LogInformation("[OpenAI] Reducing {Length} chars...", combinedSummaries.Length);
            
            var reduceClient = _openAiClient!.GetChatClient(_reduceModel);
            ChatCompletion finalCompletion = await reduceClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage("Synthesize these summaries into a cohesive report."),
                new UserChatMessage(combinedSummaries)
            }, new ChatCompletionOptions { MaxOutputTokenCount = 4096 }, cancellationToken: ct);

            return finalCompletion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DocuFlow] Summarization failed.");
            return "Error during summarization.";
        }
    }

    private string CleanDocumentText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(\r\n|\n){3,}", "\n\n");
        return text.Trim();
    }
}

public record DocumentProcessingMessage(Guid Id, string TenantId, string BlobUri, DocumentCategory Category);
