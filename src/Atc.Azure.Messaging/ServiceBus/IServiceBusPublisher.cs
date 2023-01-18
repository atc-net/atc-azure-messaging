namespace Atc.Azure.Messaging.ServiceBus;

/// <summary>
/// Publisher responsible for publishing objects with metadata to a specific ServiceBus.
/// </summary>
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