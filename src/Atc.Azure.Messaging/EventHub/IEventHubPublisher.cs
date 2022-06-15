namespace Atc.Azure.Messaging.EventHub;

public interface IEventHubPublisher : IAsyncDisposable
{
    Task PublishAsync(
        object message,
        IDictionary<string, string> messageProperties,
        CancellationToken cancellationToken = default);
}