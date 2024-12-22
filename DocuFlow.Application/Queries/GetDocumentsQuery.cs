using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using MediatR;

namespace DocuFlow.Application.Queries;

public record GetDocumentsQuery(string TenantId) : IRequest<IEnumerable<Document>>;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, IEnumerable<Document>>
{
    private readonly IDocumentRepository _repository;

    public GetDocumentsQueryHandler(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Document>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        // For now, the repository doesn't have a "GetAll" method.
        // We need to add it to the interface and implementations.
        return await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken);
    }
}
