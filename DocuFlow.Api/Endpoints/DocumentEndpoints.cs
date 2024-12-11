using DocuFlow.Application.Commands;
using DocuFlow.Application.Queries;
using DocuFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocuFlow.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/", async (InitiateUploadRequest request, IMediator mediatR, CancellationToken cancellationToken) =>
        {
            var tenantId = "tenant-123"; 

            // Pass the category from the UI request
            var command = new InitiateDocumentUploadCommand(tenantId, request.FileName, request.Category);
            var response = await mediatR.Send(command, cancellationToken);

            return Results.Ok(response);
        })
        .WithName("InitiateDocumentUpload");

        group.MapPost("/{id}/complete", async (Guid id, IMediator mediatR, CancellationToken cancellationToken) =>
        {
            var tenantId = "tenant-123";
            var command = new CompleteDocumentUploadCommand(id, tenantId);
            var success = await mediatR.Send(command, cancellationToken);

            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("CompleteDocumentUpload");

        group.MapGet("/{id}", async (Guid id, IMediator mediatR, CancellationToken cancellationToken) =>
        {
            var tenantId = "tenant-123";
            var query = new GetDocumentQuery(id, tenantId);
            var document = await mediatR.Send(query, cancellationToken);

            return document is not null ? Results.Ok(document) : Results.NotFound();
        })
        .WithName("GetDocument");
    }
}

public record InitiateUploadRequest(string FileName, DocumentCategory Category = DocumentCategory.General);
