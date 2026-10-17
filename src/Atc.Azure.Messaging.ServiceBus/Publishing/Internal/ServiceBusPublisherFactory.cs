namespace Atc.Azure.Messaging.ServiceBus.Publishing.Internal;

internal sealed class ServiceBusPublisherFactory : IServiceBusPublisherFactory
{
    private readonly IServiceBusClientFactory clientFactory;
    private readonly ServiceBusPublisherRegistration[] registrations;
    private readonly ConcurrentDictionary<(Type MessageType, string? ConnectionName), object> publishers = new();

    public ServiceBusPublisherFactory(
        IServiceBusClientFactory clientFactory,
        IEnumerable<ServiceBusPublisherRegistration> registrations)
    {
        this.clientFactory = clientFactory;
        this.registrations = [.. registrations];
    }

    public IServiceBusPublisher<TMessage> Create<TMessage>()
        => Create<TMessage>(connectionName: null);

    public IServiceBusPublisher<TMessage> Create<TMessage>(
        string? connectionName)
        => (IServiceBusPublisher<TMessage>)publishers.GetOrAdd(
            (typeof(TMessage), connectionName),
            _ => CreatePublisher<TMessage>(connectionName));

    private ServiceBusPublisher<TMessage> CreatePublisher<TMessage>(
        string? connectionName)
    {
        var registration = Array.Find(
            registrations,
            r => r.MessageType == typeof(TMessage) &&
                 string.Equals(r.ConnectionName, connectionName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No Service Bus publisher is registered for {typeof(TMessage).Name} on the connection " +
                $"{ServiceBusClientFactory.Describe(connectionName)}. Register one with AddPublisher<{typeof(TMessage).Name}>(...).");

        var builder = (ServiceBusPublisherBuilder<TMessage>)registration.Builder;
        var options = clientFactory.GetOptions(connectionName);

        return new ServiceBusPublisher<TMessage>(
            clientFactory.GetSender(connectionName, registration.TopicOrQueueName, builder.SenderOptions),
            options.Serializer,
            builder);
    }
}