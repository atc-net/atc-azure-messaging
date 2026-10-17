namespace Atc.Azure.Messaging.ServiceBus.Processing.Internal;

[ExcludeFromCodeCoverage]
internal sealed class SessionMessageSettlement : IMessageSettlement
{
    private readonly ProcessSessionMessageEventArgs args;

    public SessionMessageSettlement(ProcessSessionMessageEventArgs args)
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