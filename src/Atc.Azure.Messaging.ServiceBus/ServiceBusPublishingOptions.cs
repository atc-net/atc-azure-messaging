namespace Atc.Azure.Messaging.ServiceBus;

/// <summary>
/// Service Bus specific options for publishing a message.
/// </summary>
public class ServiceBusPublishingOptions : PublishingOptions
{
    /// <summary>
    /// Gets or sets the session the message belongs to. Required when publishing to a session enabled entity.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets how long the message lives before it expires. Defaults to the value set on the entity.
    /// </summary>
    public TimeSpan? TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets a time at which the message becomes available to receivers.
    /// </summary>
    public DateTimeOffset? ScheduledEnqueueTime { get; set; }

    /// <summary>
    /// Gets or sets an application specific label, e.g. used by subscription rules.
    /// </summary>
    public string? Subject { get; set; }

    public string? ReplyTo { get; set; }

    public string? ReplyToSessionId { get; set; }

    public string? To { get; set; }
}