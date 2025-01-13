namespace DocuFlow.Application.Interfaces;

public interface IStorageService
{
    Task<string> GenerateUploadSasUriAsync(string blobName, CancellationToken cancellationToken);
    Task<string> GenerateReadSasUriAsync(string blobName, CancellationToken cancellationToken);
    
    // Upload text content (for OCR results)
    Task<string> UploadContentAsync(string blobName, string content, string contentType, CancellationToken cancellationToken);

    // Upload binary data (for generated PDF reports)
    Task<string> UploadBytesAsync(string blobName, byte[] data, string contentType, CancellationToken cancellationToken);

    // Download the blob stream for local processing
    Task<Stream> GetBlobStreamAsync(string blobName, CancellationToken cancellationToken);
}
