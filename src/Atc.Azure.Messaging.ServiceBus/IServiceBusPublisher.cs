namespace Atc.Azure.Messaging.ServiceBus;

/// <summary>
/// Publishes messages of type <typeparamref name="TMessage"/> to a Service Bus queue or topic.
/// </summary>
/// <typeparam name="TMessage">The type of message to publish.</typeparam>
public interface IServiceBusPublisher<in TMessage> : IMessagePublisher<TMessage>
{
    /// <summary>
    /// Publishes a single message using Service Bus specific <paramref name="options"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishAsync(
        TMessage message,
        ServiceBusPublishingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages using Service Bus specific <paramref name="options"/>.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="options">Options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        ServiceBusPublishingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a message to become available to receivers at <paramref name="scheduledEnqueueTime"/>.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="scheduledEnqueueTime">The time at which the message becomes available to receivers.</param>
    /// <param name="options">Optional options overriding the defaults configured for the publisher.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>The sequence number of the scheduled message, used to cancel it.</returns>
    Task<long> SchedulePublishAsync(
        TMessage message,
        DateTimeOffset scheduledEnqueueTime,
        ServiceBusPublishingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled message that has not been enqueued yet.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number returned when the message was scheduled.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CancelScheduledPublishAsync(
        long sequenceNumber,
        CancellationToken cancellationToken = default);
}