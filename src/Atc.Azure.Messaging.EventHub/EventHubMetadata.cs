namespace Atc.Azure.Messaging.EventHub;

/// <summary>
/// Metadata about an event received from an Event Hub.
/// </summary>
public class EventHubMetadata : MessageMetadata
{
    public required string PartitionId { get; init; }

    public required long SequenceNumber { get; init; }

    public required string Offset { get; init; }

    /// <summary>
    /// Gets the properties set by the Event Hubs service, e.g. <c>x-opt-enqueued-time</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object> SystemProperties { get; init; }
        = new Dictionary<string, object>(StringComparer.Ordinal);

    public static EventHubMetadata Create(
        EventData eventData,
        string partitionId)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        return new EventHubMetadata
        {
            MessageId = eventData.MessageId,
            CorrelationId = eventData.CorrelationId,
            ContentType = eventData.ContentType,
            PartitionKey = eventData.PartitionKey,
            EnqueuedTime = eventData.EnqueuedTime,
            Properties = new Dictionary<string, object>(eventData.Properties, StringComparer.Ordinal),
            SystemProperties = eventData.SystemProperties,
            PartitionId = partitionId,
            SequenceNumber = eventData.SequenceNumber,
            Offset = eventData.OffsetString,
        };
    }
}