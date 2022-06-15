using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

public class EventHubPublisher : IEventHubPublisher
{
    private readonly EventHubProducerClient client;

    public EventHubPublisher(EventHubProducerClient client)
    {
        this.client = client;
    }

    public async Task PublishAsync(
        object message,
        IDictionary<string, string> messageProperties,
        CancellationToken cancellationToken = default)
    {
        await PerformPublishAsync(
            JsonSerializer.Serialize(message),
            messageProperties,
            cancellationToken);
    }

    private Task PerformPublishAsync(
        string messageBody,
        IDictionary<string, string> messageProperties,
        CancellationToken cancellationToken)
    {
        var eventBody = Encoding.UTF8.GetBytes(messageBody);
        var eventData = new EventData(eventBody);

        foreach (var property in messageProperties)
        {
            eventData.Properties.Add(
                property.Key,
                property.Value);
        }

        return client.SendAsync(
            new[]
            {
                eventData,
            },
            cancellationToken);
    }
}