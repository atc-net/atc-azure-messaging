namespace Atc.Azure.Messaging.EventHub;

public interface IEventHubPublisherFactory
{
    IEventHubPublisher Create(string eventHubName);
}