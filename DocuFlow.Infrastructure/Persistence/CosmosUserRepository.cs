using Microsoft.Azure.Cosmos;
using DocuFlow.Application.Interfaces;
using DocuFlow.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using User = DocuFlow.Domain.Entities.User;

namespace DocuFlow.Infrastructure.Persistence;

public class CosmosUserRepository : IUserRepository
{
    private readonly Container _container;

    public CosmosUserRepository(IConfiguration configuration, CosmosClient cosmosClient)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "DocuFlowDb";
        var containerName = "Users"; // Separate container for Users
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var queryable = _container.GetItemLinqQueryable<UserDto>();
        var iterator = queryable
            .Where(u => u.Email == email)
            .ToFeedIterator();

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            var dto = response.FirstOrDefault();
            return dto != null ? MapToEntity(dto) : null;
        }

        return null;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        try
        {
            // For Users, we might use the Email or Id as the partition key. 
            // Here, let's assume Id is the partition key for simplicity in this Silo model.
            var response = await _container.ReadItemAsync<UserDto>(
                id.ToString(), 
                new PartitionKey(id.ToString()));
            return MapToEntity(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task CreateAsync(User user)
    {
        var dto = MapToDto(user);
        await _container.CreateItemAsync(dto, new PartitionKey(dto.id));
    }

    // Cosmos DB requires a string 'id' property. Mapping to DTO to keep Domain clean.
    private UserDto MapToDto(User user) => new UserDto 
    { 
        id = user.Id.ToString(),
        Email = user.Email,
        PasswordHash = user.PasswordHash,
        FirstName = user.FirstName,
        LastName = user.LastName,
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt
    };

    private User MapToEntity(UserDto dto) => new User
    {
        Id = Guid.Parse(dto.id),
        Email = dto.Email,
        PasswordHash = dto.PasswordHash,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        TenantId = dto.TenantId,
        CreatedAt = dto.CreatedAt
    };

    private class UserDto
    {
        public string id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
