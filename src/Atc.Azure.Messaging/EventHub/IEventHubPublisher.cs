namespace Atc.Azure.Messaging.EventHub;

public interface IEventHubPublisher : IAsyncDisposable
{
    Task PublishAsync(
        object message,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default);
}