using Microsoft.Azure.Cosmos;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace DocuFlow.Infrastructure.Persistence;

public class CosmosDocumentRepository : IDocumentRepository
{
    private readonly Container _container;

    public CosmosDocumentRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CosmosDb");
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "DocuFlowDb";
        var containerName = configuration["CosmosDb:ContainerName"] ?? "Documents";

        // For local dev, ensure you have the Cosmos Emulator or a real string
        var client = new CosmosClient(connectionString);
        _container = client.GetContainer(databaseName, containerName);
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken)
    {
        await _container.CreateItemAsync(document, new PartitionKey(document.TenantId), cancellationToken: cancellationToken);
        return document;
    }

    public async Task<Document?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<Document>(
                id.ToString(), 
                new PartitionKey(tenantId), 
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        await _container.UpsertItemAsync(document, new PartitionKey(document.TenantId), cancellationToken: cancellationToken);
    }
}
