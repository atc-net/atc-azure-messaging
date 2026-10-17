namespace Atc.Azure.Messaging.StorageQueue.Builders;

/// <summary>
/// Configures a Storage Queue publisher.
/// </summary>
/// <typeparam name="TMessage">The type of message published.</typeparam>
public class StorageQueuePublisherBuilder<TMessage>
{
    internal bool CreateQueueIfNotExists { get; private set; }

    internal TimeSpan? TimeToLive { get; private set; }

    /// <summary>
    /// Creates the queue before the first message is published, if it does not exist.
    /// </summary>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueuePublisherBuilder<TMessage> CreateIfNotExists()
    {
        CreateQueueIfNotExists = true;
        return this;
    }

    /// <summary>
    /// Sets how long every message lives before it expires. Defaults to 7 days.
    /// </summary>
    /// <param name="timeToLive">The time to live, or <see cref="Timeout.InfiniteTimeSpan"/> for no expiry.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueuePublisherBuilder<TMessage> WithTimeToLive(
        TimeSpan timeToLive)
    {
        TimeToLive = timeToLive;
        return this;
    }
}