namespace Atc.Azure.Messaging.ServiceBus;

/// <summary>
/// Publisher responsible for publishing objects with metadata to a specific ServiceBus.
/// </summary>
public interface IServiceBusPublisher
{
    /// <summary>
    /// Publishes a message.
    /// </summary>
    /// <param name="topicOrQueue">The topic or queue name.</param>
    /// <param name="message">The message to be published.</param>
    /// <param name="sessionId">Optional id for appending the message to a known session. If not set, then defaults to a new session.</param>
    /// <param name="properties">Optional custom metadata about the message.</param>
    /// <param name="timeToLive">Optional <see cref="TimeSpan"/> for message to be consumed. If not set, then defaults to the value specified on queue or topic.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        string topicOrQueue,
        object message,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message.
    /// </summary>
    /// <param name="topicOrQueue">The topic or queue name.</param>
    /// <param name="message">The message to be published, in a serialized format.</param>
    /// <param name="sessionId">Optional id for appending the message to a known session. If not set, then defaults to a new session.</param>
    /// <param name="properties">Optional custom metadata about the message.</param>
    /// <param name="timeToLive">Optional <see cref="TimeSpan"/> for message to be consumed. If not set, then defaults to the value specified on queue or topic.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        string topicOrQueue,
        string message,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages in batches. The list of messages will be split in multiple batches if the messages exceeds a single batch size.
    /// </summary>
    /// <param name="topicOrQueue">The topic or queue name.</param>
    /// <param name="messages">The messages to be published.</param>
    /// <param name="sessionId">Optional id for appending the message to a known session. If not set, then defaults to a new session.</param>
    /// <param name="properties">Optional custom metadata about the message.</param>
    /// <param name="timeToLive">Optional <see cref="TimeSpan"/> for message to be consumed. If not set, then defaults to the value specified on queue or topic.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        string topicOrQueue,
        IReadOnlyCollection<object> messages,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a message for publishing at a later time.
    /// </summary>
    /// <param name="topicOrQueue">The topic or queue name.</param>
    /// <param name="message">The message to be published.</param>
    /// <param name="scheduledEnqueueTime">The time for the message to be published.</param>
    /// <param name="sessionId">Optional id for appending the message to a known session. If not set, then defaults to a new session.</param>
    /// <param name="properties">Optional custom metadata about the message.</param>
    /// <param name="timeToLive">Optional <see cref="TimeSpan"/> for message to be consumed. If not set, then defaults to the value specified on queue or topic.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> containing the sequence number of the scheduled message.</returns>
    Task<long> SchedulePublishAsync(
        string topicOrQueue,
        object message,
        DateTimeOffset scheduledEnqueueTime,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled publish of a message if it has not been published yet.
    /// </summary>
    /// <param name="topicOrQueue">The topic or queue name.</param>
    /// <param name="sequenceNumber">The sequence number of the scheduled message to cancel.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CancelScheduledPublishAsync(
        string topicOrQueue,
        long sequenceNumber,
        CancellationToken cancellationToken = default);
}