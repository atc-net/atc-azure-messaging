namespace Atc.Azure.Messaging.ServiceBus;

/// <summary>
/// Metadata about a message received from a Service Bus queue or subscription.
/// </summary>
public class ServiceBusMetadata : MessageMetadata
{
    public string? SessionId { get; init; }

    public string? Subject { get; init; }

    public string? ReplyTo { get; init; }

    public string? ReplyToSessionId { get; init; }

    public string? To { get; init; }

    /// <summary>
    /// Gets the number of times the message has been delivered, including this delivery.
    /// </summary>
    public int DeliveryCount { get; init; }

    public long SequenceNumber { get; init; }

    public long EnqueuedSequenceNumber { get; init; }

    public DateTimeOffset ScheduledEnqueueTime { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset LockedUntil { get; init; }

    public TimeSpan TimeToLive { get; init; }

    public ServiceBusMessageState State { get; init; }

    public string? DeadLetterSource { get; init; }

    public string? DeadLetterReason { get; init; }

    public string? DeadLetterErrorDescription { get; init; }

    /// <summary>
    /// Creates the metadata for a received message.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>The metadata.</returns>
    public static ServiceBusMetadata Create(ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ServiceBusMetadata
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            ContentType = message.ContentType,
            PartitionKey = message.PartitionKey,
            EnqueuedTime = message.EnqueuedTime,
            Properties = message.ApplicationProperties,
            SessionId = message.SessionId,
            Subject = message.Subject,
            ReplyTo = message.ReplyTo,
            ReplyToSessionId = message.ReplyToSessionId,
            To = message.To,
            DeliveryCount = message.DeliveryCount,
            SequenceNumber = message.SequenceNumber,
            EnqueuedSequenceNumber = message.EnqueuedSequenceNumber,
            ScheduledEnqueueTime = message.ScheduledEnqueueTime,
            ExpiresAt = message.ExpiresAt,
            LockedUntil = message.LockedUntil,
            TimeToLive = message.TimeToLive,
            State = message.State,
            DeadLetterSource = message.DeadLetterSource,
            DeadLetterReason = message.DeadLetterReason,
            DeadLetterErrorDescription = message.DeadLetterErrorDescription,
        };
    }
}