using DocuFlow.Domain.Entities;

namespace DocuFlow.Application.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken);
    Task<Document?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<Document>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken);
    Task UpdateAsync(Document document, CancellationToken cancellationToken);
}
