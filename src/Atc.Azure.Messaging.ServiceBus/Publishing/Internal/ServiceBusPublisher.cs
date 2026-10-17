namespace Atc.Azure.Messaging.ServiceBus.Publishing.Internal;

internal sealed class ServiceBusPublisher<TMessage> : IServiceBusPublisher<TMessage>
{
    private readonly ServiceBusSender sender;
    private readonly IMessageSerializer serializer;
    private readonly ServiceBusPublisherBuilder<TMessage> defaults;

    public ServiceBusPublisher(
        ServiceBusSender sender,
        IMessageSerializer serializer,
        ServiceBusPublisherBuilder<TMessage> defaults)
    {
        this.sender = sender;
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
        ServiceBusPublishingOptions options,
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
        ServiceBusPublishingOptions options,
        CancellationToken cancellationToken = default)
        => PerformPublishBatchAsync(messages, options, cancellationToken);

    public Task<long> SchedulePublishAsync(
        TMessage message,
        DateTimeOffset scheduledEnqueueTime,
        ServiceBusPublishingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return sender.ScheduleMessageAsync(
            CreateMessage(message, options),
            scheduledEnqueueTime,
            cancellationToken);
    }

    public Task CancelScheduledPublishAsync(
        long sequenceNumber,
        CancellationToken cancellationToken = default)
        => sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken);

    private Task PerformPublishAsync(
        TMessage message,
        PublishingOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return sender.SendMessageAsync(
            CreateMessage(message, options),
            cancellationToken);
    }

    private async Task PerformPublishBatchAsync(
        IReadOnlyCollection<TMessage> messages,
        PublishingOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        // Messages in a batch must share session id and partition key, so messages are grouped by both first.
        var groups = messages
            .Select(m => CreateMessage(m, options))
            .GroupBy(m => (m.SessionId, m.PartitionKey));

        foreach (var group in groups)
        {
            await SendInBatchesAsync(group, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task SendInBatchesAsync(
        IEnumerable<ServiceBusMessage> messages,
        CancellationToken cancellationToken)
    {
        var batch = await sender
            .CreateMessageBatchAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            foreach (var message in messages)
            {
                if (batch.TryAddMessage(message))
                {
                    continue;
                }

                if (batch.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"A {typeof(TMessage).Name} message is too large to fit in a Service Bus batch " +
                        $"of {batch.MaxSizeInBytes} bytes.");
                }

                await sender
                    .SendMessagesAsync(batch, cancellationToken)
                    .ConfigureAwait(false);
                batch.Dispose();

                batch = await sender
                    .CreateMessageBatchAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!batch.TryAddMessage(message))
                {
                    throw new InvalidOperationException(
                        $"A {typeof(TMessage).Name} message is too large to fit in a Service Bus batch " +
                        $"of {batch.MaxSizeInBytes} bytes.");
                }
            }

            if (batch.Count > 0)
            {
                await sender
                    .SendMessagesAsync(batch, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            batch.Dispose();
        }
    }

    private ServiceBusMessage CreateMessage(
        TMessage message,
        PublishingOptions? options)
    {
        var serviceBusMessage = new ServiceBusMessage(serializer.Serialize(message))
        {
            ContentType = serializer.ContentType,
        };

        defaults.Apply(message, serviceBusMessage);

        if (options is null)
        {
            return serviceBusMessage;
        }

        foreach (var property in options.Properties)
        {
            serviceBusMessage.ApplicationProperties[property.Key] = property.Value;
        }

        if (options.MessageId is not null)
        {
            serviceBusMessage.MessageId = options.MessageId;
        }

        if (options.CorrelationId is not null)
        {
            serviceBusMessage.CorrelationId = options.CorrelationId;
        }

        if (options.ContentType is not null)
        {
            serviceBusMessage.ContentType = options.ContentType;
        }

        if (options is ServiceBusPublishingOptions serviceBusOptions)
        {
            ApplyServiceBusOptions(serviceBusMessage, serviceBusOptions);
        }

        // Partition key is applied last, since setting a session id also resets the partition key.
        if (options.PartitionKey is not null)
        {
            serviceBusMessage.PartitionKey = options.PartitionKey;
        }

        return serviceBusMessage;
    }

    private static void ApplyServiceBusOptions(
        ServiceBusMessage serviceBusMessage,
        ServiceBusPublishingOptions options)
    {
        if (options.SessionId is not null)
        {
            serviceBusMessage.SessionId = options.SessionId;
        }

        if (options.TimeToLive is { } timeToLive)
        {
            serviceBusMessage.TimeToLive = timeToLive;
        }

        if (options.ScheduledEnqueueTime is { } scheduledEnqueueTime)
        {
            serviceBusMessage.ScheduledEnqueueTime = scheduledEnqueueTime;
        }

        if (options.Subject is not null)
        {
            serviceBusMessage.Subject = options.Subject;
        }

        if (options.ReplyTo is not null)
        {
            serviceBusMessage.ReplyTo = options.ReplyTo;
        }

        if (options.ReplyToSessionId is not null)
        {
            serviceBusMessage.ReplyToSessionId = options.ReplyToSessionId;
        }

        if (options.To is not null)
        {
            serviceBusMessage.To = options.To;
        }
    }
}