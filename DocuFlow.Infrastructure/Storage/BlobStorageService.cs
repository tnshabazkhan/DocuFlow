using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using DocuFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace DocuFlow.Infrastructure.Storage;

public class BlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _extractedContainerName;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureWebJobsStorage"] 
            ?? configuration.GetConnectionString("AzureWebJobsStorage")
            ?? "UseDevelopmentStorage=true";
            
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = configuration["Storage:ContainerName"] ?? "documents";
        _extractedContainerName = configuration["Storage:ExtractedContainerName"] ?? "extracted-data";
    }

    public async Task<string> GenerateUploadSasUriAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(blobName);
        
        // Ensure container exists before giving a SAS for a blob inside it
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Standard permissions for Block Blob upload
        sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write | BlobSasPermissions.Create | BlobSasPermissions.Add);

        // This is the most reliable way to generate the URI when using a connection string
        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task<string> GenerateReadSasUriAsync(string blobName, CancellationToken cancellationToken)
    {
        // Check if the blob starts with "extracted/" or "summaries/" to determine the container
        string container = (blobName.StartsWith("extracted/") || blobName.StartsWith("summaries/")) 
            ? _extractedContainerName 
            : _containerName;
            
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task<string> UploadContentAsync(string blobName, string content, string contentType, CancellationToken cancellationToken)
    {
        return await UploadBytesAsync(blobName, Encoding.UTF8.GetBytes(content), contentType, cancellationToken);
    }

    public async Task<string> UploadBytesAsync(string blobName, byte[] data, string contentType, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_extractedContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        
        using var stream = new MemoryStream(data);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        return blobName;
    }

    public async Task<Stream> GetBlobStreamAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task<string> GetContentAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);
        return response.Value.Content.ToString();
    }

    private BlobClient GetBlobClient(string blobName)
    {
        string container = (blobName.StartsWith("extracted/") || blobName.StartsWith("summaries/"))
            ? _extractedContainerName
            : _containerName;

        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        return containerClient.GetBlobClient(blobName);
    }
}
