namespace Atc.Azure.Messaging.StorageQueue;

/// <summary>
/// Storage Queue specific options for publishing a message.
/// </summary>
/// <remarks>
/// Storage Queues only carry the payload, so <see cref="PublishingOptions.Properties"/>, ids and
/// partition keys are ignored.
/// </remarks>
public class StorageQueuePublishingOptions : PublishingOptions
{
    /// <summary>
    /// Gets or sets how long the message stays invisible after it is published, i.e. a delivery delay.
    /// </summary>
    public TimeSpan? InitialVisibilityDelay { get; set; }

    /// <summary>
    /// Gets or sets how long the message lives before it expires. Defaults to 7 days.
    /// Use <see cref="Timeout.InfiniteTimeSpan"/> for messages that never expire.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }
}