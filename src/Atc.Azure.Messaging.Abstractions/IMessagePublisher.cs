namespace Atc.Azure.Messaging;

/// <summary>
/// Publishes messages of type <typeparamref name="TMessage"/> to the transport it was registered for.
/// </summary>
/// <typeparam name="TMessage">The type of message to publish.</typeparam>
/// <remarks>
/// Implementations are thread safe and intended to be used as singletons.
/// </remarks>
public interface IMessagePublisher<in TMessage>
{
    /// <summary>
    /// Publishes a single message.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        TMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a single message using the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        TMessage message,
        PublishingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages, using as few round trips as the transport allows.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Messages are split into as many batches as needed. Batches already sent are not rolled back
    /// if a later batch fails, and an <see cref="InvalidOperationException"/> is thrown if a single
    /// message is too large to fit in a batch by itself.
    /// </remarks>
    Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages using the specified <paramref name="options"/> for every message.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        PublishingOptions options,
        CancellationToken cancellationToken = default);
}