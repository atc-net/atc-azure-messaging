using System.Diagnostics.CodeAnalysis;
using Atc.Azure.Options.EventHub;
using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "EventHubPublisher is responsible for disposing EventHubProducerClient")]
internal sealed class EventHubPublisherFactory : IEventHubPublisherFactory
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