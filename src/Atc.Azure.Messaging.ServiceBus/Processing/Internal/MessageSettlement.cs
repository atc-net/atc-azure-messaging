namespace Atc.Azure.Messaging.ServiceBus.Processing.Internal;

[ExcludeFromCodeCoverage]
internal sealed class MessageSettlement : IMessageSettlement
{
    private readonly ProcessMessageEventArgs args;

    public MessageSettlement(ProcessMessageEventArgs args)
    {
        this.args = args;
    }

    public Task CompleteAsync(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
        => args.CompleteMessageAsync(message, cancellationToken);

    public Task AbandonAsync(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
        => args.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);

    public Task DeadLetterAsync(
        ServiceBusReceivedMessage message,
        string reason,
        string description,
        CancellationToken cancellationToken)
        => args.DeadLetterMessageAsync(message, reason, description, cancellationToken);
}