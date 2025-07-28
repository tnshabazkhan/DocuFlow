using Microsoft.Azure.Cosmos;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos.Linq;

namespace DocuFlow.Infrastructure.Persistence;

public class CosmosDocumentRepository : IDocumentRepository
{
    private readonly Container _container;

    public CosmosDocumentRepository(IConfiguration configuration, CosmosClient client)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "DocuFlowDb";
        var containerName = configuration["CosmosDb:ContainerName"] ?? "Documents";

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

    public async Task<IEnumerable<Document>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        var queryable = _container.GetItemLinqQueryable<Document>(
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(tenantId) });

        var iterator = queryable
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.UploadDate)
            .ToFeedIterator();

        var results = new List<Document>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        await _container.UpsertItemAsync(document, new PartitionKey(document.TenantId), cancellationToken: cancellationToken);
    }
}
