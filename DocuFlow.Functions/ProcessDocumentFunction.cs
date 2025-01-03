using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using System.Text;

namespace DocuFlow.Functions;

public class ProcessDocumentFunction
{
    private readonly ILogger<ProcessDocumentFunction> _logger;
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;
    private readonly DocumentIntelligenceClient _aiClient;
    private readonly AzureOpenAIClient? _openAiClient;
    private readonly string? _openAiModel;

    public ProcessDocumentFunction(
        ILogger<ProcessDocumentFunction> logger,
        IDocumentRepository repository,
        IStorageService storageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _repository = repository;
        _storageService = storageService;

        var endpoint = configuration["AI_Service_Endpoint"];
        var key = configuration["AI_Service_Key"];
        _aiClient = new DocumentIntelligenceClient(new Uri(endpoint!), new AzureKeyCredential(key!));

        var openAiEndpoint = configuration["OpenAI_Endpoint"];
        var openAiKey = configuration["OpenAI_Key"];
        _openAiModel = configuration["OpenAI_Model_Name"] ?? "gpt-4o";

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

        try
        {
            document.Status = DocumentStatus.Processing;
            await _repository.UpdateAsync(document, cancellationToken);

            string? extractedText = null;
            var fields = new Dictionary<string, object?>();

            // --- STEP 1: Determine Strategy based on Category and Extension ---
            bool isPdf = document.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            bool needsStructuredData = payload.Category == DocumentCategory.Invoice || 
                                       payload.Category == DocumentCategory.Receipt || 
                                       payload.Category == DocumentCategory.Identity;

            // --- STRATEGY 1: Local PDF Extraction (Digital PDFs) ---
            // We prioritize this for Summaries/TextExtraction to avoid Azure AI limits/costs.
            if (isPdf && !needsStructuredData)
            {
                _logger.LogInformation("[PdfPig] Attempting local text extraction for digital PDF {Id}...", document.Id);
                try
                {
                    using var blobStream = await _storageService.GetBlobStreamAsync(document.BlobUri, cancellationToken);
                    
                    // PdfPig requires a seekable stream. Blob streams are often forward-only.
                    // We copy to a MemoryStream to allow seeking.
                    using var seekableStream = new MemoryStream();
                    await blobStream.CopyToAsync(seekableStream, cancellationToken);
                    seekableStream.Position = 0;

                    using var pdf = PdfDocument.Open(seekableStream);
                    var sb = new StringBuilder();
                    int pageCount = 0;
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                        pageCount++;
                    }
                    extractedText = sb.ToString().Trim();
                    
                    if (!string.IsNullOrEmpty(extractedText) && extractedText.Length > 100)
                    {
                        _logger.LogInformation("[PdfPig] Success! Extracted {Length} chars from {PageCount} pages.", extractedText.Length, pageCount);
                        document.DocumentType = "Digital PDF";
                    }
                    else
                    {
                        _logger.LogWarning("[PdfPig] Extracted text is too short ({Length} chars). This might be a scanned PDF (images). Falling back to Azure AI OCR.", extractedText?.Length ?? 0);
                        extractedText = null; // Force fallback to Strategy 2
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[PdfPig] Failed to read PDF locally: {Message}. Falling back to Azure AI.", ex.Message);
                }
            }

            // --- STRATEGY 2: Azure AI Extraction (Photos, Scanned PDFs, or Structured Data) ---
            if (string.IsNullOrEmpty(extractedText))
            {
                string modelId = payload.Category switch
                {
                    DocumentCategory.Invoice => "prebuilt-invoice",
                    DocumentCategory.Receipt => "prebuilt-receipt",
                    DocumentCategory.Identity => "prebuilt-idDocument",
                    _ => "prebuilt-read" // Use Read for Summaries/TextExtraction fallback
                };

                _logger.LogInformation("[Azure AI] Sending to Document Intelligence using model '{Model}'...", modelId);
                
                var readUri = await _storageService.GenerateReadSasUriAsync(document.BlobUri, cancellationToken);
                var aiContent = new AnalyzeDocumentContent { UrlSource = new Uri(readUri) };
                
                var operation = await _aiClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed, 
                    modelId, 
                    aiContent, 
                    cancellationToken: cancellationToken);

                var result = operation.Value;
                extractedText = result.Content;
                
                _logger.LogInformation("[Azure AI] Analysis complete. Pages: {PageCount}. Text Length: {Length} chars.", result.Pages.Count, extractedText?.Length ?? 0);

                if (result.Pages.Count <= 2 && !string.IsNullOrEmpty(extractedText) && extractedText.Length < 1000 && payload.Category == DocumentCategory.Summary)
                {
                    _logger.LogWarning("[Azure AI] Warning: Only {PageCount} pages processed. If this is a long doc, check if you are on the FREE (F0) tier which has a 2-page limit.", result.Pages.Count);
                }

                if (result.Documents.Count > 0)
                {
                    var doc = result.Documents[0];
                    document.DocumentType = doc.DocType;
                    document.ConfidenceScore = doc.Confidence;
                    foreach (var field in doc.Fields)
                    {
                        fields.Add(field.Key, field.Value.Content);
                    }
                }
            }

            // --- STEP 3: Save Data & Generate Summary ---
            if (!string.IsNullOrEmpty(extractedText))
            {
                // Side-car storage for the full text
                string extractedBlobName = $"extracted/{document.TenantId}/{document.Id}_content.txt";
                await _storageService.UploadContentAsync(extractedBlobName, extractedText, "text/plain", cancellationToken);
                document.ExtractedTextUri = extractedBlobName;
                
                fields.Add("FullContentPreview", extractedText.Length > 2000 ? extractedText.Substring(0, 2000) + "..." : extractedText);

                // Map-Reduce Smart Summary
                if (payload.Category == DocumentCategory.Summary && _openAiClient != null)
                {
                    document.Summary = await GenerateMapReduceSummaryAsync(extractedText, cancellationToken);
                }

                document.ExtractedData = fields;
                document.Status = DocumentStatus.Processed;
                _logger.LogInformation("[DocuFlow] Successfully processed document {Id}.", document.Id);
            }
            else
            {
                _logger.LogError("[DocuFlow] Extraction failed. No text content found for document {Id}.", document.Id);
                document.Status = DocumentStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DocuFlow] Error processing document {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
        }

        await _repository.UpdateAsync(document, cancellationToken);
    }

    private async Task<string> GenerateMapReduceSummaryAsync(string fullText, CancellationToken ct)
    {
        if (_openAiClient == null) return "AI Client not configured.";

        _logger.LogInformation("[OpenAI] Starting Parallel Map-Reduce Summarization (Total Input: {Length} chars)", fullText.Length);
        
        var chatClient = _openAiClient.GetChatClient(_openAiModel!);
        
        const int chunkSize = 20000; // Increased chunk size for better context
        var mapTasks = new List<Task<string>>();
        
        // Use a Semaphore to throttle concurrency and prevent HTTP 429 (Rate Limit)
        using var semaphore = new SemaphoreSlim(5);

        for (int i = 0; i < fullText.Length; i += chunkSize)
        {
            int currentChunkIndex = (i / chunkSize) + 1;
            int totalChunks = (int)Math.Ceiling((double)fullText.Length / chunkSize);
            int length = Math.Min(chunkSize, fullText.Length - i);
            var chunk = fullText.Substring(i, length);
            
            // Create parallel tasks for each chunk
            mapTasks.Add(Task.Run(async () => 
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    _logger.LogInformation("[OpenAI] Mapping chunk {Current}/{Total}...", currentChunkIndex, totalChunks);
                    ChatCompletion completion = await chatClient.CompleteChatAsync(new ChatMessage[]
                    {
                        new SystemChatMessage("Extract and summarize every key detail, fact, and technical point from this section of a large document. Be very detailed."),
                        new UserChatMessage(chunk)
                    }, cancellationToken: ct);
                    return completion.Content[0].Text;
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        // Wait for ALL mapping tasks to complete
        var intermediateSummaries = await Task.WhenAll(mapTasks);

        _logger.LogInformation("[OpenAI] Reducing {Count} summaries into final proportional report...", intermediateSummaries.Length);
        
        var combinedSummaries = string.Join("\n\n---\n\n", intermediateSummaries);
        if (combinedSummaries.Length > 150000) combinedSummaries = combinedSummaries.Substring(0, 150000);

        string targetLengthInstruction = fullText.Length switch
        {
            < 30000 => "approximately 1 page",
            < 150000 => "approximately 2-3 pages",
            _ => "comprehensive 5-10 pages"
        };

        var options = new ChatCompletionOptions { MaxOutputTokenCount = 4096 };

        ChatCompletion finalCompletion = await chatClient.CompleteChatAsync(new ChatMessage[]
        {
            new SystemChatMessage($"You are a professional technical writer. Write a cohesive Executive Summary that is {targetLengthInstruction} in length based on the provided summaries. Use formal headings."),
            new UserChatMessage(combinedSummaries)
        }, options, cancellationToken: ct);

        return finalCompletion.Content[0].Text;
    }
}

public record DocumentProcessingMessage(Guid Id, string TenantId, string BlobUri, DocumentCategory Category);
