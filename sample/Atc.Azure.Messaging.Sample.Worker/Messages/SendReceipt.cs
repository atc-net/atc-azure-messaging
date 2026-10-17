namespace Atc.Azure.Messaging.Sample.Worker.Messages;

/// <summary>
/// A job put on a Storage Queue to send the customer a receipt.
/// </summary>
/// <param name="OrderId">The id of the order.</param>
/// <param name="CustomerId">The id of the customer.</param>
/// <param name="ShippedAt">When the order was shipped.</param>
public sealed record SendReceipt(
    Guid OrderId,
    string CustomerId,
    DateTimeOffset ShippedAt);