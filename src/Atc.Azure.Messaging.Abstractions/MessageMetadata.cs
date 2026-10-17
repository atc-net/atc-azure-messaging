namespace Atc.Azure.Messaging;

/// <summary>
/// Transport agnostic metadata about a received message.
/// </summary>
/// <remarks>
/// Each transport provides a derived type with transport specific details.
/// </remarks>
public class MessageMetadata
{
    public string? MessageId { get; init; }

    public string? CorrelationId { get; init; }

    public string? ContentType { get; init; }

    public string? PartitionKey { get; init; }

    public DateTimeOffset EnqueuedTime { get; init; }

    /// <summary>
    /// Gets the custom application properties sent with the message.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; }
        = new Dictionary<string, object>(StringComparer.Ordinal);
}