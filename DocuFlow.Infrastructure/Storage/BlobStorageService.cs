using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocuFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocuFlow.Infrastructure.Storage;

public class BlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        // For local dev, this will hit Azurite on localhost:10000
        var connectionString = configuration["AzureWebJobsStorage"] ?? configuration.GetConnectionString("AzureWebJobsStorage") 
            ?? "UseDevelopmentStorage=true";
            
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = configuration["Storage:ContainerName"] ?? "documents";
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
        var blobClient = GetBlobClient(blobName);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    private BlobClient GetBlobClient(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        return containerClient.GetBlobClient(blobName);
    }
}
