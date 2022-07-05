using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

public class EventHubMessagePublisher : IMessagePublisher
{
    private readonly EventHubProducerClient client;

    public EventHubMessagePublisher(EventHubProducerClient client)
        => this.client = client;

    public Task PublishAsync(
        object message,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
