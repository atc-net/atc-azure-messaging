namespace Atc.Azure.Messaging.Sample.Worker.Messages;

/// <summary>
/// An event published to Event Hubs when a customer places an order.
/// </summary>
/// <param name="OrderId">The id of the order.</param>
/// <param name="CustomerId">The id of the customer, used as partition key.</param>
/// <param name="Amount">The total amount of the order.</param>
public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerId,
    decimal Amount);