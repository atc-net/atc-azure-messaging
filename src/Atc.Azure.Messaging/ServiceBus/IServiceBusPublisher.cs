namespace Atc.Azure.Messaging.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishAsync(
        string topicOrQueue,
        object message,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);
}