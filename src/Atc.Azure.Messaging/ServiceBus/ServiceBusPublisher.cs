namespace Atc.Azure.Messaging.ServiceBus;

internal sealed class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly IServiceBusSenderProvider clientProvider;

    public ServiceBusPublisher(IServiceBusSenderProvider clientProvider)
    {
        this.clientProvider = clientProvider;
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
                    JsonSerializer.Serialize(message),
                    properties,
                    timeToLive),
                cancellationToken);
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
                    JsonSerializer.Serialize(message),
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