namespace Atc.Azure.Messaging.ServiceBus.Processing.Internal;

/// <summary>
/// Dispatches received messages and settles them according to the outcome.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
internal sealed class ServiceBusMessageHandler<TMessage, TProcessor>
    where TProcessor : class, IMessageProcessor<TMessage>
{
    internal const string DeserializationFailedReason = "DeserializationFailed";

    private readonly MessageDispatcher<TMessage, TProcessor, ServiceBusMetadata> dispatcher;

    public ServiceBusMessageHandler(
        MessageDispatcher<TMessage, TProcessor, ServiceBusMetadata> dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    /// <summary>
    /// Processes a message, then completes, abandons or dead-letters it.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="settlement">Settles the message on the receiver it was received from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task HandleAsync(
        ServiceBusReceivedMessage message,
        IMessageSettlement settlement,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher
            .DispatchAsync(message.Body, ServiceBusMetadata.Create(message), cancellationToken)
            .ConfigureAwait(false);

        switch (result)
        {
            case MessageDispatchResult.Processed:
            case MessageDispatchResult.Filtered:
                await settlement
                    .CompleteAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case MessageDispatchResult.DeserializationFailed:
                // Retrying will not make the payload readable, so the message goes straight to the dead-letter queue.
                await settlement
                    .DeadLetterAsync(
                        message,
                        DeserializationFailedReason,
                        $"The message could not be deserialized into {typeof(TMessage).Name}.",
                        cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                // The broker redelivers the message, and dead-letters it once MaxDeliveryCount is reached.
                await settlement
                    .AbandonAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Reports an error raised by the Service Bus processor, e.g. a connection or lock renewal failure.
    /// </summary>
    /// <param name="args">The error event arguments.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return dispatcher.ReportErrorAsync(args.Exception, metadata: null, args.CancellationToken);
    }
}