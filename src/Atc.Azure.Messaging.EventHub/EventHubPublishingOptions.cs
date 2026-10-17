namespace Atc.Azure.Messaging.EventHub;

/// <summary>
/// Event Hub specific options for publishing a message.
/// </summary>
public class EventHubPublishingOptions : PublishingOptions
{
    /// <summary>
    /// Gets or sets the partition to publish to. Cannot be combined with
    /// <see cref="PublishingOptions.PartitionKey"/>.
    /// </summary>
    public string? PartitionId { get; set; }
}