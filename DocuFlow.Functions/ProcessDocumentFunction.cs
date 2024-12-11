using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Enums;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocuFlow.Functions;

public class ProcessDocumentFunction
{
    private readonly ILogger<ProcessDocumentFunction> _logger;
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;
    private readonly DocumentIntelligenceClient _aiClient;

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
    }

    [Function(nameof(ProcessDocumentFunction))]
    public async Task Run(
        [ServiceBusTrigger("docuflow", Connection = "ServiceBusConnection")]
        string messageBody,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing message: {message}", messageBody);

        var payload = JsonSerializer.Deserialize<DocumentProcessingMessage>(messageBody);
        if (payload == null) return;

        var document = await _repository.GetByIdAsync(payload.Id, payload.TenantId, cancellationToken);
        if (document == null) return;

        try
        {
            document.Status = DocumentStatus.Processing;
            await _repository.UpdateAsync(document, cancellationToken);

            // Select Model based on Category
            string modelId = payload.Category switch
            {
                DocumentCategory.Invoice => "prebuilt-invoice",
                DocumentCategory.Receipt => "prebuilt-receipt",
                DocumentCategory.Identity => "prebuilt-idDocument",
                DocumentCategory.TextExtraction => "prebuilt-read",
                DocumentCategory.Summary => "prebuilt-read", // We'll add LLM summary later
                _ => "prebuilt-layout" // Default for General
            };

            var readUri = await _storageService.GenerateReadSasUriAsync(document.BlobUri, cancellationToken);
            var content = new AnalyzeDocumentContent { UrlSource = new Uri(readUri) };
            
            _logger.LogInformation("Analyzing document {Id} using model {Model}...", document.Id, modelId);

            var operation = await _aiClient.AnalyzeDocumentAsync(
                WaitUntil.Completed, 
                modelId, 
                content, 
                cancellationToken: cancellationToken);

            var result = operation.Value;

            if (result.Documents.Count > 0 || result.Content != null)
            {
                var fields = new Dictionary<string, object?>();

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
                else
                {
                    // Fallback for Read/Layout models that don't have "Documents" but have "Content"
                    fields.Add("FullContent", result.Content);
                    document.DocumentType = "Generic";
                }

                document.ExtractedData = fields;
                document.Status = DocumentStatus.Processed;
                _logger.LogInformation("Successfully processed document {Id}.", document.Id);
            }
            else
            {
                document.Status = DocumentStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
        }

        await _repository.UpdateAsync(document, cancellationToken);
    }
}

public record DocumentProcessingMessage(Guid Id, string TenantId, string BlobUri, DocumentCategory Category);
