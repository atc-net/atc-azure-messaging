namespace Atc.Azure.Messaging.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishAsync(
        string topicOrQueue,
        string sessionId,
        string messageBody,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);
}