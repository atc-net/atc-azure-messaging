namespace Atc.Azure.Messaging.StorageQueue.Publishing.Internal;

internal sealed class StorageQueuePublisherFactory : IStorageQueuePublisherFactory
{
    private readonly IStorageQueueClientFactory clientFactory;
    private readonly StorageQueuePublisherRegistration[] registrations;
    private readonly ConcurrentDictionary<(Type MessageType, string? ConnectionName), object> publishers = new();

    public StorageQueuePublisherFactory(
        IStorageQueueClientFactory clientFactory,
        IEnumerable<StorageQueuePublisherRegistration> registrations)
    {
        this.clientFactory = clientFactory;
        this.registrations = [.. registrations];
    }

    public IStorageQueuePublisher<TMessage> Create<TMessage>()
        => Create<TMessage>(connectionName: null);

    public IStorageQueuePublisher<TMessage> Create<TMessage>(
        string? connectionName)
        => (IStorageQueuePublisher<TMessage>)publishers.GetOrAdd(
            (typeof(TMessage), connectionName),
            _ => CreatePublisher<TMessage>(connectionName));

    private StorageQueuePublisher<TMessage> CreatePublisher<TMessage>(
        string? connectionName)
    {
        var registration = Array.Find(
            registrations,
            r => r.MessageType == typeof(TMessage) &&
                 string.Equals(r.ConnectionName, connectionName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No Storage Queue publisher is registered for {typeof(TMessage).Name} on the connection " +
                $"{StorageQueueClientFactory.Describe(connectionName)}. Register one with AddPublisher<{typeof(TMessage).Name}>(...).");

        var options = clientFactory.GetOptions(connectionName);

        return new StorageQueuePublisher<TMessage>(
            clientFactory.GetQueueClient(connectionName, registration.QueueName),
            options.Serializer,
            (StorageQueuePublisherBuilder<TMessage>)registration.Builder);
    }
}