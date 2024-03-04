using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.ServiceBus;

internal sealed class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly IServiceBusSenderProvider clientProvider;
    private readonly IMessagePayloadSerializer messagePayloadSerializer;

    public ServiceBusPublisher(
        IServiceBusSenderProvider clientProvider,
        IMessagePayloadSerializer messagePayloadSerializer)
    {
        this.clientProvider = clientProvider;
        this.messagePayloadSerializer = messagePayloadSerializer;
    }

    public Task PublishAsync(
        string topicOrQueue,
        object message,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        return clientProvider
            .GetSender(topicOrQueue)
            .SendMessageAsync(
                CreateServiceBusMessage(
                    sessionId,
                    messagePayloadSerializer.Serialize(message),
                    properties,
                    timeToLive),
                cancellationToken);
    }

    public Task PublishAsync(
        string topicOrQueue,
        string message,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        return clientProvider
            .GetSender(topicOrQueue)
            .SendMessageAsync(
                CreateServiceBusMessage(
                    sessionId,
                    message,
                    properties,
                    timeToLive),
                cancellationToken);
    }

    public async Task PublishAsync(
        string topicOrQueue,
        IReadOnlyCollection<object> messages,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        var sender = clientProvider.GetSender(topicOrQueue);

        var batch = await sender
            .CreateMessageBatchAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var busMessage = CreateServiceBusMessage(
                sessionId,
                messagePayloadSerializer.Serialize(message),
                properties,
                timeToLive);

            if (batch.TryAddMessage(busMessage))
            {
                continue;
            }

            await sender
                .SendMessagesAsync(batch, cancellationToken)
                .ConfigureAwait(false);

            batch.Dispose();
            batch = await sender
                .CreateMessageBatchAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!batch.TryAddMessage(busMessage))
            {
                throw new InvalidOperationException(
                    "Unable to add message to batch. The message size exceeds what can be send in a batch");
            }
        }

        await sender
            .SendMessagesAsync(batch, cancellationToken)
            .ConfigureAwait(false);

        batch.Dispose();
    }

    public Task<long> SchedulePublishAsync(
        string topicOrQueue,
        object message,
        DateTimeOffset scheduledEnqueueTime,
        string? sessionId = null,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        return clientProvider
            .GetSender(topicOrQueue)
            .ScheduleMessageAsync(
                CreateServiceBusMessage(
                    sessionId,
                    messagePayloadSerializer.Serialize(message),
                    properties,
                    timeToLive),
                scheduledEnqueueTime,
                cancellationToken);
    }

    public Task CancelScheduledPublishAsync(
        string topicOrQueue,
        long sequenceNumber,
        CancellationToken cancellationToken = default)
    {
        return clientProvider
            .GetSender(topicOrQueue)
            .CancelScheduledMessageAsync(
                sequenceNumber,
                cancellationToken);
    }

    private static ServiceBusMessage CreateServiceBusMessage(
        string? sessionId,
        string messageBody,
        IDictionary<string, string>? properties,
        TimeSpan? timeToLive)
    {
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
        };

        if (timeToLive != null)
        {
            message.TimeToLive = timeToLive.Value;
        }

        if (properties == null)
        {
            return message;
        }

        foreach (var (key, value) in properties)
        {
            message.ApplicationProperties[key] = value;
        }

        return message;
    }
}