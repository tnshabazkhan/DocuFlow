namespace DocuFlow.Application.DTOs;

public record InitiateUploadResponse(Guid DocumentId, string SasUri);
