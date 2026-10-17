namespace Atc.Azure.Messaging;

/// <summary>
/// Processes messages of type <typeparamref name="TMessage"/> received from a transport.
/// </summary>
/// <typeparam name="TMessage">The type of message to process.</typeparam>
/// <remarks>
/// Implement <see cref="IProcessErrorHandler"/> on the same class to handle processing errors yourself.
/// </remarks>
public interface IMessageProcessor<in TMessage>
{
    /// <summary>
    /// Processes a received message.
    /// </summary>
    /// <param name="message">The deserialized message.</param>
    /// <param name="metadata">
    /// Metadata about the received message. Cast it to the transport specific type,
    /// e.g. <c>ServiceBusMetadata</c>, for transport specific details.
    /// </param>
    /// <param name="cancellationToken">
    /// Canceled when processing stops, e.g. on shutdown or when an Event Hub partition is handed to another instance.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ProcessAsync(
        TMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken);
}