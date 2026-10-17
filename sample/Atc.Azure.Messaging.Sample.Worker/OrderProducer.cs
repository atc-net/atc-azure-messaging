namespace Atc.Azure.Messaging.Sample.Worker;

/// <summary>
/// Places an order every few seconds. It only depends on <see cref="IMessagePublisher{TMessage}"/>,
/// so it does not know which transport carries the message.
/// </summary>
public sealed partial class OrderProducer : BackgroundService
{
    private static readonly string[] Customers = ["alice", "bob", "carol"];

    private readonly IMessagePublisher<OrderPlaced> publisher;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OrderProducer> logger;

    public OrderProducer(
        IMessagePublisher<OrderPlaced> publisher,
        TimeProvider timeProvider,
        ILogger<OrderProducer> logger)
    {
        this.publisher = publisher;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5), timeProvider);
        var orderNumber = 0;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var order = new OrderPlaced(
                Guid.NewGuid(),
                Customers[orderNumber++ % Customers.Length],
                Amount: 100 + orderNumber);

            await publisher.PublishAsync(order, stoppingToken);
            LogOrderPlaced(order.OrderId, order.CustomerId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Placed order {OrderId} for {CustomerId}")]
    private partial void LogOrderPlaced(
        Guid orderId,
        string customerId);
}