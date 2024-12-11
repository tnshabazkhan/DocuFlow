using DocuFlow.Application.Interfaces;
using DocuFlow.Infrastructure.Messaging;
using DocuFlow.Infrastructure.Persistence;
using DocuFlow.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Use Cosmos DB for shared state between API and Functions
        var cosmosConn = configuration.GetConnectionString("CosmosDb");
        if (!string.IsNullOrEmpty(cosmosConn) && !cosmosConn.Contains("fake"))
        {
            services.AddSingleton<IDocumentRepository, CosmosDocumentRepository>();
        }
        else
        {
            services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        }
        
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

        return services;
    }
}
