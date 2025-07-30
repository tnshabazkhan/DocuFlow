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

        services.AddSingleton(sp => new CosmosClient(cosmosConn));
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
}
