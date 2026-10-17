namespace Atc.Azure.Messaging.StorageQueue.Publishing.Internal;

internal sealed class StorageQueuePublisher<TMessage> : IStorageQueuePublisher<TMessage>
{
    private readonly QueueClient queue;
    private readonly IMessageSerializer serializer;
    private readonly StorageQueuePublisherBuilder<TMessage> defaults;
    private Task? createQueueTask;

    public StorageQueuePublisher(
        QueueClient queue,
        IMessageSerializer serializer,
        StorageQueuePublisherBuilder<TMessage> defaults)
    {
        this.queue = queue;
        this.serializer = serializer;
        this.defaults = defaults;
    }

    public Task PublishAsync(
        TMessage message,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync([message], options: null, cancellationToken);

    public Task PublishAsync(
        TMessage message,
        PublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync([message], options as StorageQueuePublishingOptions, cancellationToken);

    public Task PublishAsync(
        TMessage message,
        StorageQueuePublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync([message], options, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(messages, options: null, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        PublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(messages, options as StorageQueuePublishingOptions, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        StorageQueuePublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(messages, options, cancellationToken);

    private async Task PerformPublishAsync(
        IReadOnlyCollection<TMessage> messages,
        StorageQueuePublishingOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (defaults.CreateQueueIfNotExists)
        {
            await EnsureQueueExists()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var message in messages)
        {
            if (message is null)
            {
                throw new ArgumentException("The messages cannot contain null.", nameof(messages));
            }

            await queue
                .SendMessageAsync(
                    serializer.Serialize(message),
                    options?.InitialVisibilityDelay,
                    options?.TimeToLive ?? defaults.TimeToLive,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private Task EnsureQueueExists()
    {
        // Creating a queue is idempotent, so a race between publishers is harmless. A failed attempt is retried.
        var task = createQueueTask;
        if (task is null || task.IsFaulted || task.IsCanceled)
        {
            task = queue.CreateIfNotExistsAsync(cancellationToken: CancellationToken.None);
            createQueueTask = task;
        }

        return task;
    }
}