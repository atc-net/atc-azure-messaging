namespace Atc.Azure.Messaging.EventHub;

internal sealed class EventHubPublisher : IEventHubPublisher
{
    private readonly EventHubProducerClient client;

    public EventHubPublisher(EventHubProducerClient client)
    {
        this.client = client;
    }

    public Task PublishAsync(
        object message,
        IDictionary<string, string>? messageProperties = null,
        CancellationToken cancellationToken = default)
    {
        return PerformPublishAsync(
            JsonSerializer.Serialize(message),
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
            new[]
            {
                eventData,
            },
            cancellationToken);
    }

    public ValueTask DisposeAsync() => client.DisposeAsync();
}