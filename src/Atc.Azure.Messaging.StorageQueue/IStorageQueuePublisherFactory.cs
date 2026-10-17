namespace Atc.Azure.Messaging.StorageQueue;

/// <summary>
/// Creates publishers for message types registered with <c>AddPublisher</c>.
/// </summary>
public interface IStorageQueuePublisherFactory
{
    /// <summary>
    /// Creates the publisher for <typeparamref name="TMessage"/> on the default connection.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <returns>The publisher, which is cached and safe to use as a singleton.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no publisher is registered for <typeparamref name="TMessage"/> on the connection.
    /// </exception>
    IStorageQueuePublisher<TMessage> Create<TMessage>();

    /// <summary>
    /// Creates the publisher for <typeparamref name="TMessage"/> on the specified connection.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <returns>The publisher, which is cached and safe to use as a singleton.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no publisher is registered for <typeparamref name="TMessage"/> on the connection.
    /// </exception>
    IStorageQueuePublisher<TMessage> Create<TMessage>(string? connectionName);
}