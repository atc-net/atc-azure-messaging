namespace Atc.Azure.Messaging.StorageQueue.Providers.Internal;

internal sealed class StorageQueueClientFactory : IStorageQueueClientFactory
{
    private readonly IOptionsMonitor<StorageQueueMessagingOptions> optionsMonitor;
    private readonly ConcurrentDictionary<string, QueueServiceClient> clients = new(StringComparer.Ordinal);

    public StorageQueueClientFactory(
        IOptionsMonitor<StorageQueueMessagingOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
    }

    public StorageQueueMessagingOptions GetOptions(string? connectionName)
    {
        var options = optionsMonitor.Get(connectionName);
        if (options.ConnectionString is null &&
            (options.QueueServiceUri is null || options.Credential is null))
        {
            throw new InvalidOperationException(
                $"The Storage Queue connection {Describe(connectionName)} is not configured. " +
                "Set a QueueServiceUri and Credential, or a ConnectionString.");
        }

        return options;
    }

    public QueueClient GetQueueClient(
        string? connectionName,
        string queueName)
        => clients
            .GetOrAdd(connectionName ?? string.Empty, _ => CreateServiceClient(connectionName))
            .GetQueueClient(queueName);

    internal static string Describe(string? connectionName)
        => connectionName is null
            ? "(default)"
            : $"'{connectionName}'";

    private QueueServiceClient CreateServiceClient(string? connectionName)
    {
        var options = GetOptions(connectionName);
        var clientOptions = options.ClientOptions ?? new QueueClientOptions();

        return options.ConnectionString is { } connectionString
            ? new QueueServiceClient(connectionString, clientOptions)
            : new QueueServiceClient(options.QueueServiceUri, options.Credential, clientOptions);
    }
}