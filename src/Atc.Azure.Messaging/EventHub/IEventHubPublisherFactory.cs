using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

public interface IEventHubPublisherFactory
{
    IEventHubPublisher Create(string eventHubName);

    EventHubProducerClient CreateEventHubProducerClient(string eventHubName);
}