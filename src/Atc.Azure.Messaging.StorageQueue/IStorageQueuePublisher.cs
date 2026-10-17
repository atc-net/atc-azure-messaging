namespace Atc.Azure.Messaging.StorageQueue;

/// <summary>
/// Publishes messages of type <typeparamref name="TMessage"/> to a Storage Queue.
/// </summary>
/// <typeparam name="TMessage">The type of message to publish.</typeparam>
public interface IStorageQueuePublisher<in TMessage> : IMessagePublisher<TMessage>
{
    /// <summary>
    /// Publishes a single message using Storage Queue specific <paramref name="options"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        TMessage message,
        StorageQueuePublishingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages using Storage Queue specific <paramref name="options"/>.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Storage Queues have no batch API, so messages are sent one at a time, in order.
    /// </remarks>
    Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        StorageQueuePublishingOptions options,
        CancellationToken cancellationToken = default);
}