namespace Atc.Azure.Messaging.Sample.Worker.Messages;

/// <summary>
/// A command sent to a Service Bus queue to ship an order.
/// </summary>
/// <param name="OrderId">The id of the order.</param>
/// <param name="CustomerId">The id of the customer.</param>
public sealed record ShipOrder(
    Guid OrderId,
    string CustomerId);