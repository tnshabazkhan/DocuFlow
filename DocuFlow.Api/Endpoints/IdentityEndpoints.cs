using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DocuFlow.Api.Endpoints;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity").WithTags("Identity");

        group.MapPost("/register", async (
            [FromBody] RegisterUserRequest request,
            IUserRepository repository,
            IJwtService jwtService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("IdentityEndpoints");
            try
            {
                var existingUser = await repository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Results.BadRequest(new { error = "User already exists" });
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    TenantId = Guid.NewGuid().ToString().Substring(0, 8) // Generate a unique tenantId for this user
                };

                await repository.CreateAsync(user);

                var token = jwtService.GenerateToken(user);

                return Results.Ok(new AuthResponse(
                    user.Id, 
                    user.Email, 
                    user.FirstName, 
                    user.LastName, 
                    user.TenantId, 
                    token));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during registration for {Email}", request.Email);
                return Results.InternalServerError(new { error = ex.Message });
            }
        })
        .WithName("RegisterUser");

        group.MapPost("/login", async (
            [FromBody] LoginUserRequest request,
            IUserRepository repository,
            IJwtService jwtService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("IdentityEndpoints");
            try
            {
                logger.LogInformation("Login attempt for {Email}", request.Email);
                var user = await repository.GetByEmailAsync(request.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    logger.LogWarning("Invalid login attempt for {Email}", request.Email);
                    return Results.Unauthorized();
                }

                var token = jwtService.GenerateToken(user);

                return Results.Ok(new AuthResponse(
                    user.Id, 
                    user.Email, 
                    user.FirstName, 
                    user.LastName, 
                    user.TenantId, 
                    token));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login for {Email}", request.Email);
                return Results.InternalServerError(new { error = ex.Message });
            }
        })
        .WithName("LoginUser");
    }
}

public record RegisterUserRequest(string Email, string Password, string FirstName, string LastName);
public record LoginUserRequest(string Email, string Password);
public record AuthResponse(Guid Id, string Email, string FirstName, string LastName, string TenantId, string Token);
