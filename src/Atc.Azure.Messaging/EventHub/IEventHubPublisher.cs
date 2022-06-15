namespace Atc.Azure.Messaging.EventHub;

public interface IEventHubPublisher
{
    Task PublishAsync(
        object message,
        IDictionary<string, string> messageProperties,
        CancellationToken cancellationToken = default);
}