using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using MediatR;

namespace DocuFlow.Application.Queries;

public record GetDocumentQuery(Guid Id, string TenantId) : IRequest<Document?>;

public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, Document?>
{
    private readonly IDocumentRepository _repository;

    public GetDocumentQueryHandler(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Document?> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
    }
}
