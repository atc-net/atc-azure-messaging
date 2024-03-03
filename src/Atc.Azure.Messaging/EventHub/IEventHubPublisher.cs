namespace Atc.Azure.Messaging.EventHub;

/// <summary>
/// Publisher responsible for publishing objects with metadata to a specific EventHub.
/// </summary>
/// <remarks>
/// Is safe to cache and use in singletons for the lifetime of the application.
/// </remarks>
public interface IEventHubPublisher : IAsyncDisposable
{
    Task PublishAsync(
        object message,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        string message,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default);
}