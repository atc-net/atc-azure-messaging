namespace Atc.Azure.Messaging;

public interface IMessagePublisherBuilder
{
    IMessagePublisherBuilder AddEventHub(string eventHubName);

    IMessagePublisherBuilder AddServiceBusTopic(string topicName);

    IMessagePublisherBuilder AddServiceBusQueue(string queueName);

    IMessagePublisher Build();
}
