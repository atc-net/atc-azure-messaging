using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public class ServiceBusMessagePublisher : IMessagePublisher
{
    private readonly ServiceBusSender sender;

    public ServiceBusMessagePublisher(ServiceBusSender sender)
    {
        this.sender = sender;
    }

    public Task PublishAsync(
        object message,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
        => sender
            .SendMessageAsync(
                CreateServiceBusMessage(
                    JsonSerializer.Serialize(message),
                    properties,
                    timeToLive),
                cancellationToken);

    private static ServiceBusMessage CreateServiceBusMessage(
        string messageBody,
        IDictionary<string, string>? properties,
        TimeSpan? timeToLive)
    {
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
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
