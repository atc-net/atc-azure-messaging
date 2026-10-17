namespace Atc.Azure.Messaging.ServiceBus.Processing.Internal;

/// <summary>
/// Settles a received message on the receiver it was received from.
/// </summary>
internal interface IMessageSettlement
{
    Task CompleteAsync(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken);

    Task AbandonAsync(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken);

    Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        string reason,
        string description,
        CancellationToken cancellationToken);
}