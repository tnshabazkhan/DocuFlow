using DocuFlow.Application.DTOs;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using DocuFlow.Domain.Enums;
using MediatR;

namespace DocuFlow.Application.Commands;

public record InitiateDocumentUploadCommand(string TenantId, string FileName, DocumentCategory Category) : IRequest<InitiateUploadResponse>;

public class InitiateDocumentUploadCommandHandler : IRequestHandler<InitiateDocumentUploadCommand, InitiateUploadResponse>
{
    private readonly IDocumentRepository _repository;
    private readonly IStorageService _storageService;

    public InitiateDocumentUploadCommandHandler(
        IDocumentRepository repository, 
        IStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<InitiateUploadResponse> Handle(InitiateDocumentUploadCommand request, CancellationToken cancellationToken)
    {
        // 1. Create the document entity
        var document = new Document
        {
            TenantId = request.TenantId,
            FileName = request.FileName,
            Category = request.Category,
            BlobUri = $"{request.TenantId}/raw/{Guid.NewGuid()}_{request.FileName}"
        };

        await _repository.AddAsync(document, cancellationToken);

        // 2. Ask the Storage service for a direct-upload URL
        var sasUri = await _storageService.GenerateUploadSasUriAsync(document.BlobUri, cancellationToken);

        return new InitiateUploadResponse(document.Id, sasUri);
    }
}
