namespace Atc.Azure.Messaging.ServiceBus.Providers.Internal;

internal sealed class ServiceBusClientFactory : IServiceBusClientFactory, IAsyncDisposable
{
    private readonly IOptionsMonitor<ServiceBusMessagingOptions> optionsMonitor;
    private readonly ConcurrentDictionary<string, ServiceBusClient> clients = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string? ConnectionName, string EntityName), ServiceBusSender> senders = new();

    public ServiceBusClientFactory(
        IOptionsMonitor<ServiceBusMessagingOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
    }

    public ServiceBusMessagingOptions GetOptions(string? connectionName)
    {
        var options = optionsMonitor.Get(connectionName);
        if (options.ConnectionString is null &&
            (options.FullyQualifiedNamespace is null || options.Credential is null))
        {
            throw new InvalidOperationException(
                $"The Service Bus connection {Describe(connectionName)} is not configured. " +
                "Set a FullyQualifiedNamespace and Credential, or a ConnectionString.");
        }

        return options;
    }

    public ServiceBusClient GetClient(string? connectionName)
        => clients.GetOrAdd(
            connectionName ?? string.Empty,
            _ => CreateClient(connectionName));

    public ServiceBusSender GetSender(
        string? connectionName,
        string topicOrQueueName,
        ServiceBusSenderOptions? senderOptions)
        => senders.GetOrAdd(
            (connectionName, topicOrQueueName),
            key => GetClient(key.ConnectionName).CreateSender(key.EntityName, senderOptions));

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in senders.Values)
        {
            await sender
                .DisposeAsync()
                .ConfigureAwait(false);
        }

        foreach (var client in clients.Values)
        {
            await client
                .DisposeAsync()
                .ConfigureAwait(false);
        }

        senders.Clear();
        clients.Clear();
    }

    internal static string Describe(string? connectionName)
        => connectionName is null
            ? "(default)"
            : $"'{connectionName}'";

    private ServiceBusClient CreateClient(string? connectionName)
    {
        var options = GetOptions(connectionName);
        var clientOptions = options.ClientOptions ?? new ServiceBusClientOptions();

        return options.ConnectionString is { } connectionString
            ? new ServiceBusClient(connectionString, clientOptions)
            : new ServiceBusClient(options.FullyQualifiedNamespace, options.Credential, clientOptions);
    }
}