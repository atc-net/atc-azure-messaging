namespace Atc.Azure.Messaging.EventHub;

/// <summary>
/// Publishes messages of type <typeparamref name="TMessage"/> to an Event Hub.
/// </summary>
/// <typeparam name="TMessage">The type of message to publish.</typeparam>
public interface IEventHubPublisher<in TMessage> : IMessagePublisher<TMessage>
{
    /// <summary>
    /// Publishes a single message using Event Hub specific <paramref name="options"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    Task PublishAsync(
        TMessage message,
        EventHubPublishingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages using Event Hub specific <paramref name="options"/>.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        EventHubPublishingOptions options,
        CancellationToken cancellationToken = default);
}