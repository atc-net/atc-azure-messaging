namespace Atc.Azure.Messaging.EventHub.Publishing.Internal;

internal sealed class EventHubPublisherFactory : IEventHubPublisherFactory
{
    private readonly IEventHubClientFactory clientFactory;
    private readonly EventHubPublisherRegistration[] registrations;
    private readonly ConcurrentDictionary<(Type MessageType, string? ConnectionName), object> publishers = new();

    public EventHubPublisherFactory(
        IEventHubClientFactory clientFactory,
        IEnumerable<EventHubPublisherRegistration> registrations)
    {
        this.clientFactory = clientFactory;
        this.registrations = [.. registrations];
    }

    public IEventHubPublisher<TMessage> Create<TMessage>()
        => Create<TMessage>(connectionName: null);

    public IEventHubPublisher<TMessage> Create<TMessage>(string? connectionName)
        => (IEventHubPublisher<TMessage>)publishers.GetOrAdd(
            (typeof(TMessage), connectionName),
            _ => CreatePublisher<TMessage>(connectionName));

    private EventHubPublisher<TMessage> CreatePublisher<TMessage>(
        string? connectionName)
    {
        var registration = Array.Find(
            registrations,
            r => r.MessageType == typeof(TMessage) &&
                 string.Equals(r.ConnectionName, connectionName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No Event Hub publisher is registered for {typeof(TMessage).Name} on the connection " +
                $"{EventHubClientFactory.Describe(connectionName)}. Register one with AddPublisher<{typeof(TMessage).Name}>(...).");

        var options = clientFactory.GetOptions(connectionName);

        return new EventHubPublisher<TMessage>(
            clientFactory.GetProducerClient(connectionName, registration.EventHubName),
            options.Serializer,
            (EventHubPublisherBuilder<TMessage>)registration.Builder);
    }
}