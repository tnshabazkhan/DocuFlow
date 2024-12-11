namespace DocuFlow.Application.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string queueOrTopicName, CancellationToken cancellationToken);
}
