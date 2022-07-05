using Atc.Azure.Options.EventHub;
using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

#pragma warning disable CA2000 // Dispose objects before losing scope

public class EventHubPublisherFactory : IEventHubPublisherFactory
{
    private readonly string connectionString;

    public EventHubPublisherFactory(EventHubOptions options)
    {
        this.connectionString = options.ConnectionString;
    }

    public IEventHubPublisher Create(string eventHubName)
        => new EventHubPublisher(CreateEventHubProducerClient(eventHubName));

    public EventHubProducerClient CreateEventHubProducerClient(string eventHubName)
        => new EventHubProducerClient(connectionString, eventHubName);
}