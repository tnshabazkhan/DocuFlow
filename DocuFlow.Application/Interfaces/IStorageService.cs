namespace DocuFlow.Application.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Generates a short-lived Shared Access Signature (SAS) URL for direct client-to-blob upload.
    /// This prevents the API from becoming a bottleneck for large files.
    /// </summary>
    Task<string> GenerateUploadSasUriAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a short-lived read-only SAS URL so external services (like Azure AI) can access the file.
    /// </summary>
    Task<string> GenerateReadSasUriAsync(string blobName, CancellationToken cancellationToken);
}
