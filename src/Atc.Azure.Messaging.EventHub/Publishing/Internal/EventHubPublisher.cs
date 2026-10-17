namespace Atc.Azure.Messaging.EventHub.Publishing.Internal;

internal sealed class EventHubPublisher<TMessage> : IEventHubPublisher<TMessage>
{
    private readonly EventHubProducerClient producer;
    private readonly IMessageSerializer serializer;
    private readonly EventHubPublisherBuilder<TMessage> defaults;

    public EventHubPublisher(
        EventHubProducerClient producer,
        IMessageSerializer serializer,
        EventHubPublisherBuilder<TMessage> defaults)
    {
        this.producer = producer;
        this.serializer = serializer;
        this.defaults = defaults;
    }

    public Task PublishAsync(
        TMessage message,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(message, options: null, cancellationToken);

    public Task PublishAsync(
        TMessage message,
        PublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(message, options, cancellationToken);

    public Task PublishAsync(
        TMessage message,
        EventHubPublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishAsync(message, options, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken cancellationToken = default)
        => PerformPublishBatchAsync(messages, options: null, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        PublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishBatchAsync(messages, options, cancellationToken);

    public Task PublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        EventHubPublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishBatchAsync(messages, options, cancellationToken);

    private Task PerformPublishAsync(
        TMessage message,
        PublishingOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventData = CreateEventData(message, options);
        var sendOptions = new SendEventOptions
        {
            PartitionId = (options as EventHubPublishingOptions)?.PartitionId,
            PartitionKey = GetPartitionKey(message, options),
        };

        return producer.SendAsync([eventData], sendOptions, cancellationToken);
    }

    private async Task PerformPublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        PublishingOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        // An Event Hubs batch targets a single partition key, so messages are grouped by key first.
        var partitionId = (options as EventHubPublishingOptions)?.PartitionId;
        var groups = messages.GroupBy(m => GetPartitionKey(m, options), StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var batchOptions = new CreateBatchOptions
            {
                PartitionId = partitionId,
                PartitionKey = group.Key,
            };

            await SendInBatchesAsync(group, options, batchOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task SendInBatchesAsync(
        IEnumerable<TMessage> messages,
        PublishingOptions? options,
        CreateBatchOptions batchOptions,
        CancellationToken cancellationToken)
    {
        var batch = await producer
            .CreateBatchAsync(batchOptions, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            foreach (var message in messages)
            {
                var eventData = CreateEventData(message, options);
                if (batch.TryAdd(eventData))
                {
                    continue;
                }

                if (batch.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"A {typeof(TMessage).Name} message is too large to fit in an Event Hubs batch " +
                        $"of {batch.MaximumSizeInBytes} bytes.");
                }

                await producer
                    .SendAsync(batch, cancellationToken)
                    .ConfigureAwait(false);
                batch.Dispose();

                batch = await producer
                    .CreateBatchAsync(batchOptions, cancellationToken)
                    .ConfigureAwait(false);

                if (!batch.TryAdd(eventData))
                {
                    throw new InvalidOperationException(
                        $"A {typeof(TMessage).Name} message is too large to fit in an Event Hubs batch " +
                        $"of {batch.MaximumSizeInBytes} bytes.");
                }
            }

            if (batch.Count > 0)
            {
                await producer
                    .SendAsync(batch, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            batch.Dispose();
        }
    }

    private EventData CreateEventData(
        TMessage message,
        PublishingOptions? options)
    {
        var eventData = new EventData(serializer.Serialize(message))
        {
            ContentType = serializer.ContentType,
        };

        defaults.Apply(message, eventData);

        if (options is null)
        {
            return eventData;
        }

        foreach (var property in options.Properties)
        {
            eventData.Properties[property.Key] = property.Value;
        }

        if (options.MessageId is not null)
        {
            eventData.MessageId = options.MessageId;
        }

        if (options.CorrelationId is not null)
        {
            eventData.CorrelationId = options.CorrelationId;
        }

        if (options.ContentType is not null)
        {
            eventData.ContentType = options.ContentType;
        }

        return eventData;
    }

    private string? GetPartitionKey(
        TMessage message,
        PublishingOptions? options)
    {
        if (options is EventHubPublishingOptions { PartitionId: not null })
        {
            return null;
        }

        return options?.PartitionKey ?? defaults.PartitionKeySelector?.Invoke(
           message);
    }
}