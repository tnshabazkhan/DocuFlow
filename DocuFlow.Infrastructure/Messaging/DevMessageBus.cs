using System.Text.Json;
using DocuFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFlow.Infrastructure.Messaging;

/// <summary>
/// A "Mock" message bus for local development. 
/// It logs the message to the console instead of throwing a connection error.
/// This allows the API to function (returning SAS URIs) even without a real Service Bus.
/// </summary>
public class DevMessageBus : IMessageBus
{
    private readonly ILogger<DevMessageBus> _logger;

    public DevMessageBus(ILogger<DevMessageBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, string queueOrTopicName, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
        
        _logger.LogWarning("""
            [DEV MESSAGE BUS]
            Topic/Queue: {Queue}
            Message: {Message}
            Note: To enable real background processing, provide a valid 'ServiceBus' connection string in appsettings.json.
            """, queueOrTopicName, json);

        return Task.CompletedTask;
    }
}
