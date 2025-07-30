using DocuFlow.Application.Interfaces;
using DocuFlow.Infrastructure.Authentication;
using DocuFlow.Infrastructure.Messaging;
using DocuFlow.Infrastructure.Persistence;
using DocuFlow.Infrastructure.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cosmosConn = configuration.GetConnectionString("CosmosDb") 
            ?? throw new InvalidOperationException("Cosmos DB connection string is missing.");

        services.AddSingleton(sp => 
        {
            var client = new CosmosClient(cosmosConn);
            
            // Simple initialization for Development
            InitializeCosmosAsync(client, configuration).GetAwaiter().GetResult();
            
            return client;
        });

        services.AddSingleton<IDocumentRepository, CosmosDocumentRepository>();
        services.AddSingleton<IUserRepository, CosmosUserRepository>();
        
        // Register the Blob Storage Service
        services.AddScoped<IStorageService, BlobStorageService>();

        // Register the Message Bus
        var serviceBusConn = configuration.GetConnectionString("ServiceBus");
        if (!string.IsNullOrEmpty(serviceBusConn) && !serviceBusConn.Contains("fake"))
        {
            services.AddScoped<IMessageBus, ServiceBusMessageBus>();
        }
        else
        {
            services.AddScoped<IMessageBus, DevMessageBus>();
        }

        // Identity and Auth
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }

    private static async Task InitializeCosmosAsync(CosmosClient client, IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? "DocuFlowDb";
        var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        
        // Create Documents container
        var documentsContainerName = configuration["CosmosDb:ContainerName"] ?? "Documents";
        await database.Database.CreateContainerIfNotExistsAsync(documentsContainerName, "/tenantId");
        
        // Create Users container
        await database.Database.CreateContainerIfNotExistsAsync("Users", "/id");
    }
}
