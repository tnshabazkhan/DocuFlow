using System.Text.Json;
using DocuFlow.Domain.Enums;
using Newtonsoft.Json;

namespace DocuFlow.Domain.Entities;

public class Document
{
    [JsonProperty("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;
    
    public string BlobUri { get; set; } = string.Empty;
    
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    // Added Category to track which AI model to use
    public DocumentCategory Category { get; set; } = DocumentCategory.General;
    
    public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
    
    public string? DocumentType { get; set; } 
    
    public double? ConfidenceScore { get; set; }
    
    public Dictionary<string, object?>? ExtractedData { get; set; }
}
