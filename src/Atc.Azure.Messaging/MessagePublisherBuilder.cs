using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging;

public class MessagePublisherBuilder : IMessagePublisherBuilder
{
    private readonly IEventHubPublisherFactory eventHubPublisherFactory;
    private readonly IServiceBusPublisher serviceBusPublisher;
    private readonly IReadOnlyCollection<IMessagePublisher> publishers = null!;

    public MessagePublisherBuilder(
        IEventHubPublisherFactory eventHubPublisherFactory,
        IServiceBusPublisher serviceBusPublisher)
    {
        this.eventHubPublisherFactory = eventHubPublisherFactory;
        this.serviceBusPublisher = serviceBusPublisher;
    }

    private MessagePublisherBuilder(
        IEventHubPublisherFactory eventHubPublisherFactory,
        IServiceBusPublisher serviceBusPublisher,
        IEnumerable<IMessagePublisher> publishers)
        : this(eventHubPublisherFactory, serviceBusPublisher)
    {
        this.publishers = publishers.ToList();
    }

    public IMessagePublisherBuilder AddEventHub(string eventHubName)
        => new MessagePublisherBuilder(
            eventHubPublisherFactory,
            serviceBusPublisher,
            publishers.Append(
                new EventHubMessagePublisher(
                    eventHubPublisherFactory.Create(eventHubName))));

    public IMessagePublisherBuilder AddServiceBusQueue(string queueName)
        => new MessagePublisherBuilder(
            eventHubPublisherFactory,
            serviceBusPublisher,
            publishers.Append(
                new ServiceBusMessagePublisher(
                    serviceBusPublisher.);

    public IMessagePublisherBuilder AddServiceBusTopic(string topicName)
    {
        throw new NotImplementedException();
    }

    public IMessagePublisher Build()
    {
        throw new NotImplementedException();
    }
}