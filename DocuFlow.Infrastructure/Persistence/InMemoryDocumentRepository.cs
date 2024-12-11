using System.Collections.Concurrent;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;

namespace DocuFlow.Infrastructure.Persistence;

/// <summary>
/// A temporary in-memory repository to allow rapid API development.
/// We will swap this out for Cosmos DB once the async queue is built.
/// </summary>
public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public Task<Document> AddAsync(Document document, CancellationToken cancellationToken)
    {
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task<Document?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken)
    {
        if (_documents.TryGetValue(id, out var document) && document.TenantId == tenantId)
        {
            return Task.FromResult<Document?>(document);
        }
        return Task.FromResult<Document?>(null);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }
}
