using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.EventHub;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "EventHubPublisher is responsible for disposing EventHubProducerClient")]
internal sealed class EventHubPublisherFactory : IEventHubPublisherFactory
{
    private readonly string connectionString;
    private readonly IMessagePayloadSerializer messagePayloadSerializer;

    public EventHubPublisherFactory(
        EventHubOptions options,
        IMessagePayloadSerializer messagePayloadSerializer)
    {
        this.messagePayloadSerializer = messagePayloadSerializer;
        connectionString = options.ConnectionString;
    }

    public IEventHubPublisher Create(string eventHubName)
        => new EventHubPublisher(
            new EventHubProducerClient(
                connectionString,
                eventHubName),
            messagePayloadSerializer);
}