namespace Atc.Azure.Messaging.EventHub.Providers.Internal;

internal sealed class EventHubClientFactory : IEventHubClientFactory, IAsyncDisposable
{
    private readonly IOptionsMonitor<EventHubMessagingOptions> optionsMonitor;
    private readonly ConcurrentDictionary<(string? ConnectionName, string EventHubName), EventHubProducerClient> producers = new();

    public EventHubClientFactory(
        IOptionsMonitor<EventHubMessagingOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
    }

    public EventHubMessagingOptions GetOptions(string? connectionName)
    {
        var options = optionsMonitor.Get(connectionName);
        if (options.ConnectionString is null &&
            (options.FullyQualifiedNamespace is null || options.Credential is null))
        {
            throw new InvalidOperationException(
                $"The Event Hubs connection {Describe(connectionName)} is not configured. " +
                "Set a FullyQualifiedNamespace and Credential, or a ConnectionString.");
        }

        return options;
    }

    public EventHubProducerClient GetProducerClient(
        string? connectionName,
        string eventHubName)
        => producers.GetOrAdd(
            (connectionName, eventHubName),
            key => CreateProducerClient(key.ConnectionName, key.EventHubName));

    public EventHubConsumerClient CreateConsumerClient(
        string? connectionName,
        string eventHubName,
        string consumerGroup)
    {
        var options = GetOptions(connectionName);
        var clientOptions = options.ConsumerClientOptions ?? new EventHubConsumerClientOptions();

        return options.ConnectionString is { } connectionString
            ? new EventHubConsumerClient(consumerGroup, connectionString, eventHubName, clientOptions)
            : new EventHubConsumerClient(consumerGroup, options.FullyQualifiedNamespace!, eventHubName, options.Credential!, clientOptions);
    }

    public BlobContainerClient CreateCheckpointContainerClient(
        string? connectionName,
        string containerName)
    {
        var options = GetOptions(connectionName);
        var serviceClient = options.CheckpointStorage switch
        {
            { ConnectionString: { } connectionString } storage
                => new BlobServiceClient(connectionString, storage.ClientOptions),
            { ServiceUri: { } serviceUri } storage when (storage.Credential ?? options.Credential) is { } credential
                => new BlobServiceClient(serviceUri, credential, storage.ClientOptions),
            _ => throw new InvalidOperationException(
                $"Checkpoint storage is not configured for the Event Hubs connection {Describe(connectionName)}. " +
                "Call WithCheckpointStorage(...) when configuring the connection."),
        };

        return serviceClient.GetBlobContainerClient(containerName);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var producer in producers.Values)
        {
            await producer
                .DisposeAsync()
                .ConfigureAwait(false);
        }

        producers.Clear();
    }

    internal static string Describe(string? connectionName)
        => connectionName is null
            ? "(default)"
            : $"'{connectionName}'";

    private EventHubProducerClient CreateProducerClient(
        string? connectionName,
        string eventHubName)
    {
        var options = GetOptions(connectionName);
        var clientOptions = options.ProducerClientOptions ?? new EventHubProducerClientOptions();

        return options.ConnectionString is { } connectionString
            ? new EventHubProducerClient(connectionString, eventHubName, clientOptions)
            : new EventHubProducerClient(options.FullyQualifiedNamespace!, eventHubName, options.Credential!, clientOptions);
    }
}