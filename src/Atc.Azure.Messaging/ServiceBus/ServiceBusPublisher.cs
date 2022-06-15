using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public class ServiceBusPublisher : IServiceBusPublisher
{
    private readonly IServiceBusSenderProvider clientProvider;

    public ServiceBusPublisher(IServiceBusSenderProvider clientProvider)
    {
        this.clientProvider = clientProvider;
    }

    public async Task PublishAsync(
        string topicOrQueue,
        string sessionId,
        string messageBody,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        await PerformPublishAsync(
            topicOrQueue,
            sessionId,
            messageBody,
            properties,
            timeToLive,
            cancellationToken);
    }

    private Task PerformPublishAsync(
        string topicOrQueue,
        string sessionId,
        string messageBody,
        IDictionary<string, string>? properties,
        TimeSpan? timeToLive,
        CancellationToken cancellationToken)
    {
        var message = CreateServiceBusMessage(
            sessionId,
            messageBody,
            properties,
            timeToLive);

        return clientProvider
            .GetSender(topicOrQueue)
            .SendMessageAsync(message, cancellationToken);
    }

    private static ServiceBusMessage CreateServiceBusMessage(
        string sessionId,
        string messageBody,
        IDictionary<string, string>? properties,
        TimeSpan? timeToLive)
    {
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
            SessionId = sessionId,
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