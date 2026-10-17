namespace Atc.Azure.Messaging;

/// <summary>
/// Options for publishing a message. Values set here override the defaults configured for the publisher.
/// </summary>
/// <remarks>
/// Each transport provides a derived type with transport specific options.
/// Values a transport cannot carry, such as properties on a Storage Queue, are ignored.
/// </remarks>
public class PublishingOptions
{
    public string? MessageId { get; set; }

    public string? CorrelationId { get; set; }

    public string? ContentType { get; set; }

    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets custom application properties to send with the message.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; }
        = new Dictionary<string, object>(StringComparer.Ordinal);
}