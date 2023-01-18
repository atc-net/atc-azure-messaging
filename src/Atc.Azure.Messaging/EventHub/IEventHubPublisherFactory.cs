namespace Atc.Azure.Messaging.EventHub;

/// <summary>
/// Factory responsible for creating <see cref="IEventHubPublisher"/>s for a specific EventHub namespace.
/// </summary>
public interface IEventHubPublisherFactory
{
    IEventHubPublisher Create(string eventHubName);
}