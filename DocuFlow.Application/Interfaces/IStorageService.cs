namespace DocuFlow.Application.Interfaces;

public interface IStorageService
{
    Task<string> GenerateUploadSasUriAsync(string blobName, CancellationToken cancellationToken);
    Task<string> GenerateReadSasUriAsync(string blobName, CancellationToken cancellationToken);
    
    // New method to upload extracted JSON/Text directly from the backend
    Task<string> UploadContentAsync(string blobName, string content, string contentType, CancellationToken cancellationToken);

    // New method to download the blob stream for local processing (e.g. PDF text extraction)
    Task<Stream> GetBlobStreamAsync(string blobName, CancellationToken cancellationToken);
}
