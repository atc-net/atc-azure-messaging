namespace Atc.Azure.Messaging.StorageQueue;

/// <summary>
/// Metadata about a message received from a Storage Queue.
/// </summary>
public class StorageQueueMetadata : MessageMetadata
{
    /// <summary>
    /// Gets the name of the queue the message was received from.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Gets the number of times the message has been received, including this time.
    /// </summary>
    public long DequeueCount { get; init; }

    public DateTimeOffset? ExpiresOn { get; init; }

    public DateTimeOffset? NextVisibleOn { get; init; }

    public required string PopReceipt { get; init; }

    /// <summary>
    /// Creates the metadata for a received message.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <returns>The metadata.</returns>
    public static StorageQueueMetadata Create(
        QueueMessage message,
        string queueName)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new StorageQueueMetadata
        {
            MessageId = message.MessageId,
            EnqueuedTime = message.InsertedOn ?? default,
            QueueName = queueName,
            DequeueCount = message.DequeueCount,
            ExpiresOn = message.ExpiresOn,
            NextVisibleOn = message.NextVisibleOn,
            PopReceipt = message.PopReceipt,
        };
    }
}