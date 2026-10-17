namespace Atc.Azure.Messaging.Sample.Worker.Processors;

/// <summary>
/// Ships orders from the Service Bus queue and puts a <see cref="SendReceipt"/> job on a Storage Queue.
/// Implementing <see cref="IProcessErrorHandler"/> replaces the default error logging.
/// </summary>
public sealed partial class ShipOrderProcessor : IMessageProcessor<ShipOrder>, IProcessErrorHandler
{
    private readonly IMessagePublisher<SendReceipt> publisher;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ShipOrderProcessor> logger;

    public ShipOrderProcessor(
        IMessagePublisher<SendReceipt> publisher,
        TimeProvider timeProvider,
        ILogger<ShipOrderProcessor> logger)
    {
        this.publisher = publisher;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task ProcessAsync(
        ShipOrder message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        LogShipping(message.OrderId, message.CustomerId);

        return publisher.PublishAsync(
            new SendReceipt(message.OrderId, message.CustomerId, timeProvider.GetUtcNow()),
            cancellationToken);
    }

    public Task ProcessErrorAsync(
        Exception exception,
        MessageMetadata? metadata,
        CancellationToken cancellationToken)
    {
        LogShippingFailed(exception, metadata?.MessageId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shipping order {OrderId} to {CustomerId}")]
    private partial void LogShipping(
        Guid orderId,
        string customerId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Shipping failed for message {MessageId}")]
    private partial void LogShippingFailed(
        Exception exception,
        string? messageId);
}