namespace Atc.Azure.Messaging.StorageQueue.Processing.Internal;

internal sealed record StorageQueueProcessorSettings(
    TimeSpan PollingInterval,
    int MaxMessages,
    TimeSpan VisibilityTimeout,
    int MaxDequeueCount,
    bool CreateQueuesIfNotExist)
{
    /// <summary>
    /// Gets the delay before receiving is retried after a failure.
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);
}