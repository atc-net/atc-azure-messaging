using Atc.Azure.Options.EventHub;
using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

public class EventHubPublisherFactory : IEventHubPublisherFactory
{
    private readonly string connectionString;

    public EventHubPublisherFactory(EventHubOptions options)
    {
        this.connectionString = options.ConnectionString;
    }

    public IEventHubPublisher Create(string eventHubName)
        => new EventHubPublisher(
            new EventHubProducerClient(
                connectionString,
                eventHubName));
}