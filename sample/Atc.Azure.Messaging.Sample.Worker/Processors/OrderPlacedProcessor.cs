namespace Atc.Azure.Messaging.Sample.Worker.Processors;

/// <summary>
/// Reads placed orders from Event Hubs and sends a <see cref="ShipOrder"/> command to Service Bus.
/// </summary>
public sealed partial class OrderPlacedProcessor : IMessageProcessor<OrderPlaced>
{
    private readonly IMessagePublisher<ShipOrder> publisher;
    private readonly ILogger<OrderPlacedProcessor> logger;

    public OrderPlacedProcessor(
        IMessagePublisher<ShipOrder> publisher,
        ILogger<OrderPlacedProcessor> logger)
    {
        this.publisher = publisher;
        this.logger = logger;
    }

    public Task ProcessAsync(
        OrderPlaced message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        var partitionId = (metadata as EventHubMetadata)?.PartitionId;
        LogOrderReceived(message.OrderId, partitionId);

        return publisher.PublishAsync(
            new ShipOrder(message.OrderId, message.CustomerId),
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Received order {OrderId} from partition {PartitionId}")]
    private partial void LogOrderReceived(
        Guid orderId,
        string? partitionId);
}