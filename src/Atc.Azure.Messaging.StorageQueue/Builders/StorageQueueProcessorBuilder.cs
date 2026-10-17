namespace Atc.Azure.Messaging.StorageQueue.Builders;

/// <summary>
/// Configures a Storage Queue processor.
/// </summary>
public class StorageQueueProcessorBuilder
{
    internal const int MaxMessagesLimit = 32;

    internal StorageQueueProcessorBuilder(string queueName)
    {
        PoisonQueueName = $"{queueName}-poison";
    }

    internal TimeSpan PollingInterval { get; private set; } = TimeSpan.FromSeconds(5);

    internal int MaxMessages { get; private set; } = 16;

    internal TimeSpan VisibilityTimeout { get; private set; } = TimeSpan.FromSeconds(30);

    internal int MaxDequeueCount { get; private set; } = 5;

    internal string PoisonQueueName { get; private set; }

    internal bool CreateQueuesIfNotExist { get; private set; }

    /// <summary>
    /// Sets how long to wait before polling again when the queue is empty. Defaults to 5 seconds.
    /// </summary>
    /// <param name="pollingInterval">How long to wait before polling again when the queue is empty.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder WithPollingInterval(
        TimeSpan pollingInterval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pollingInterval, TimeSpan.Zero);
        PollingInterval = pollingInterval;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of messages received per poll. Defaults to 16.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages received per poll, between 1 and 32.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder WithMaxMessages(int maxMessages)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxMessages, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxMessages, MaxMessagesLimit);
        MaxMessages = maxMessages;
        return this;
    }

    /// <summary>
    /// Sets how long a received message stays invisible to other receivers. A message that is not processed within
    /// this time is received again. Defaults to 30 seconds.
    /// </summary>
    /// <param name="visibilityTimeout">How long a received message stays invisible to other receivers while it is processed.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder WithVisibilityTimeout(
        TimeSpan visibilityTimeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(visibilityTimeout, TimeSpan.Zero);
        VisibilityTimeout = visibilityTimeout;
        return this;
    }

    /// <summary>
    /// Sets the number of failed attempts after which a message is moved to the poison queue. Defaults to 5.
    /// </summary>
    /// <param name="maxDequeueCount">The number of failed attempts after which a message is moved to the poison queue.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder WithMaxDequeueCount(int maxDequeueCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDequeueCount, 1);
        MaxDequeueCount = maxDequeueCount;
        return this;
    }

    /// <summary>
    /// Sets the queue that failed messages are moved to. Defaults to the queue name followed by <c>-poison</c>.
    /// </summary>
    /// <param name="poisonQueueName">The name of the poison queue.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder WithPoisonQueue(string poisonQueueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(poisonQueueName);
        PoisonQueueName = poisonQueueName;
        return this;
    }

    /// <summary>
    /// Creates the queue and the poison queue when processing starts, if they do not exist.
    /// </summary>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueProcessorBuilder CreateIfNotExists()
    {
        CreateQueuesIfNotExist = true;
        return this;
    }
}