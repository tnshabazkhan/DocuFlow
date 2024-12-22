using DocuFlow.Application.Interfaces;
using MediatR;

namespace DocuFlow.Application.Queries;

public record GetDocumentContentUrlQuery(Guid Id, string TenantId) : IRequest<string?>;

public class GetDocumentContentUrlQueryHandler : IRequestHandler<GetDocumentContentUrlQuery, string?>
{
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;

    public GetDocumentContentUrlQueryHandler(IDocumentRepository repository, IStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<string?> Handle(GetDocumentContentUrlQuery request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
        
        if (document == null || string.IsNullOrEmpty(document.ExtractedTextUri))
        {
            return null;
        }

        // Generate a temporary read-only SAS URI for the extracted text file
        return await _storageService.GenerateReadSasUriAsync(document.ExtractedTextUri, cancellationToken);
    }
}
