namespace Atc.Azure.Messaging.Sample.Worker.Processors;

/// <summary>
/// Sends receipts for the jobs on the Storage Queue.
/// </summary>
public sealed partial class SendReceiptProcessor : IMessageProcessor<SendReceipt>
{
    private readonly ILogger<SendReceiptProcessor> logger;

    public SendReceiptProcessor(ILogger<SendReceiptProcessor> logger)
    {
        this.logger = logger;
    }

    public Task ProcessAsync(
        SendReceipt message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        LogReceiptSent(message.OrderId, message.CustomerId, message.ShippedAt);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent receipt for order {OrderId} to {CustomerId}, shipped at {ShippedAt}")]
    private partial void LogReceiptSent(
        Guid orderId,
        string customerId,
        DateTimeOffset shippedAt);
}