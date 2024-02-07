using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.EventHub;

internal sealed class EventHubPublisher : IEventHubPublisher
{
    private readonly EventHubProducerClient client;
    private readonly IMessagePayloadSerializer messagePayloadSerializer;

    public EventHubPublisher(
        EventHubProducerClient client,
        IMessagePayloadSerializer messagePayloadSerializer)
    {
        this.client = client;
        this.messagePayloadSerializer = messagePayloadSerializer;
    }

    public Task PublishAsync(
        object message,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default)
    {
        return PerformPublishAsync(
            messagePayloadSerializer.Serialize(message),
            messageProperties,
            cancellationToken);
    }

    private Task PerformPublishAsync(
        string messageBody,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default)
    {
        var eventBody = Encoding.UTF8.GetBytes(messageBody);
        var eventData = new EventData(eventBody);

        if (messageProperties != null)
        {
            foreach (var property in messageProperties)
            {
                eventData.Properties.Add(
                    property.Key,
                    property.Value);
            }
        }

        return client.SendAsync(
            new[] { eventData, },
            cancellationToken);
    }

    public ValueTask DisposeAsync() => client.DisposeAsync();
}