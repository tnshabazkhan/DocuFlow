using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
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
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public async Task<string> GenerateReadSasUriAsync(string blobName, CancellationToken cancellationToken)
    {
        string container = blobName.StartsWith("extracted/") ? _extractedContainerName : _containerName;
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
        var containerClient = _blobServiceClient.GetBlobContainerClient(_extractedContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        return blobName;
    }

    public async Task<Stream> GetBlobStreamAsync(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    private BlobClient GetBlobClient(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        return containerClient.GetBlobClient(blobName);
    }
}
