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
        var cosmosConn = configuration.GetConnectionString("CosmosDb");
        bool useRealCosmos = !string.IsNullOrEmpty(cosmosConn) && !cosmosConn.Contains("fake");

        if (useRealCosmos)
        {
            services.AddSingleton(sp => new CosmosClient(cosmosConn));
            services.AddSingleton<IDocumentRepository, CosmosDocumentRepository>();
            services.AddSingleton<IUserRepository, CosmosUserRepository>();
        }
        else
        {
            services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
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

        // Identity and Auth
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
