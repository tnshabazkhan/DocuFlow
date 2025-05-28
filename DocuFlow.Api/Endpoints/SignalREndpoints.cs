using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;

namespace DocuFlow.Api.Endpoints;

public static class SignalREndpoints
{
    public static void MapSignalREndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/realtime").WithTags("Realtime");

        group.MapPost("/negotiate", async (ServiceHubContext hubContext, CancellationToken cancellationToken) =>
        {
            // Note: In a real app, we'd use the authenticated user's ID as the userId
            var userId = "user-123"; 
            
            var negotiationResponse = await hubContext.NegotiateAsync(new NegotiationOptions { UserId = userId }, cancellationToken);
            
            return Results.Ok(new
            {
                url = negotiationResponse.Url,
                accessToken = negotiationResponse.AccessToken
            });
        })
        .WithName("NegotiateSignalR");
    }
}
