using System.Text.Json;
using Azure.Messaging.ServiceBus;
using DocuFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocuFlow.Infrastructure.Messaging;

public class ServiceBusMessageBus : IMessageBus
{
    private readonly ServiceBusClient _client;

    public ServiceBusMessageBus(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus") 
                               ?? "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fake";
        
        _client = new ServiceBusClient(connectionString);
    }

    public async Task PublishAsync<T>(T message, string queueOrTopicName, CancellationToken cancellationToken)
    {
        var sender = _client.CreateSender(queueOrTopicName);
        var json = JsonSerializer.Serialize(message);
        var busMessage = new ServiceBusMessage(json);

        await sender.SendMessageAsync(busMessage, cancellationToken);
    }
}
