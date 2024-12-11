using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Enums;
using MediatR;

namespace DocuFlow.Application.Commands;

public record CompleteDocumentUploadCommand(Guid DocumentId, string TenantId) : IRequest<bool>;

public class CompleteDocumentUploadCommandHandler : IRequestHandler<CompleteDocumentUploadCommand, bool>
{
    private readonly IDocumentRepository _repository;
    private readonly IMessageBus _messageBus;

    public CompleteDocumentUploadCommandHandler(IDocumentRepository repository, IMessageBus messageBus)
    {
        _repository = repository;
        _messageBus = messageBus;
    }

    public async Task<bool> Handle(CompleteDocumentUploadCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.DocumentId, request.TenantId, cancellationToken);
        
        if (document == null) return false;

        document.Status = DocumentStatus.Uploaded;
        await _repository.UpdateAsync(document, cancellationToken);

        // Notify with Category included
        await _messageBus.PublishAsync(new 
        { 
            document.Id, 
            document.TenantId, 
            document.BlobUri, 
            document.Category 
        }, "docuflow", cancellationToken);

        return true;
    }
}
