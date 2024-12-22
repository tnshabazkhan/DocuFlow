using System.Collections.Concurrent;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;

namespace DocuFlow.Infrastructure.Persistence;

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

    public Task<IEnumerable<Document>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        var docs = _documents.Values.Where(d => d.TenantId == tenantId).OrderByDescending(d => d.UploadDate);
        return Task.FromResult<IEnumerable<Document>>(docs);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken)
    {
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }
}
