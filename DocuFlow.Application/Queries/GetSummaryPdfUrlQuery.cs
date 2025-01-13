using DocuFlow.Application.Interfaces;
using MediatR;

namespace DocuFlow.Application.Queries;

public record GetSummaryPdfUrlQuery(Guid Id, string TenantId) : IRequest<string?>;

public class GetSummaryPdfUrlQueryHandler : IRequestHandler<GetSummaryPdfUrlQuery, string?>
{
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;

    public GetSummaryPdfUrlQueryHandler(IDocumentRepository repository, IStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<string?> Handle(GetSummaryPdfUrlQuery request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
        
        if (document == null || string.IsNullOrEmpty(document.SummaryPdfUri))
        {
            return null;
        }

        // Generate a temporary read-only SAS URI for the summary PDF file
        return await _storageService.GenerateReadSasUriAsync(document.SummaryPdfUri, cancellationToken);
    }
}
