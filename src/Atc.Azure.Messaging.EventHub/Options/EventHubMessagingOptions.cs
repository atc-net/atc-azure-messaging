namespace Atc.Azure.Messaging.EventHub.Options;

/// <summary>
/// Options for an Event Hubs connection. A connection is identified by its name,
/// where <see langword="null"/> is the default connection.
/// </summary>
/// <remarks>
/// Set either <see cref="FullyQualifiedNamespace"/> and <see cref="Credential"/>, or <see cref="ConnectionString"/>.
/// </remarks>
public class EventHubMessagingOptions
{
    /// <summary>
    /// Gets or sets the fully qualified Event Hubs namespace, e.g. <c>my-namespace.servicebus.windows.net</c>.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the credential used together with <see cref="FullyQualifiedNamespace"/>,
    /// e.g. a <c>DefaultAzureCredential</c> or <c>ManagedIdentityCredential</c>.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a connection string for the Event Hubs namespace.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the serializer used for message payloads. Defaults to <see cref="JsonMessageSerializer"/>.
    /// </summary>
    public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

    /// <summary>
    /// Gets or sets options for the producer clients used by publishers, e.g. to tune
    /// <see cref="EventHubProducerClientOptions.RetryOptions"/> so publishing rides out throttling.
    /// </summary>
    public EventHubProducerClientOptions? ProducerClientOptions { get; set; }

    /// <summary>
    /// Gets or sets options for the consumer clients used by stateless processors.
    /// </summary>
    public EventHubConsumerClientOptions? ConsumerClientOptions { get; set; }

    /// <summary>
    /// Gets or sets the Blob Storage account used to store checkpoints and partition ownership.
    /// Only required by checkpointed processors.
    /// </summary>
    public CheckpointStorageOptions? CheckpointStorage { get; set; }

    public EventHubMessagingOptions WithConnection(
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        FullyQualifiedNamespace = fullyQualifiedNamespace;
        Credential = credential;
        return this;
    }

    public EventHubMessagingOptions WithConnectionString(
        string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public EventHubMessagingOptions WithSerializer(
        IMessageSerializer serializer)
    {
        Serializer = serializer;
        return this;
    }

    public EventHubMessagingOptions WithJsonSerializerOptions(
        JsonSerializerOptions serializerOptions)
    {
        Serializer = new JsonMessageSerializer(serializerOptions);
        return this;
    }

    public EventHubMessagingOptions WithProducerClientOptions(
        EventHubProducerClientOptions options)
    {
        ProducerClientOptions = options;
        return this;
    }

    public EventHubMessagingOptions WithConsumerClientOptions(
        EventHubConsumerClientOptions options)
    {
        ConsumerClientOptions = options;
        return this;
    }

    /// <summary>
    /// Stores checkpoints in the Blob Storage account at <paramref name="serviceUri"/>.
    /// </summary>
    /// <param name="serviceUri">The blob service endpoint, e.g. <c>https://account.blob.core.windows.net</c>.</param>
    /// <param name="credential">
    /// The credential for the storage account, or <see langword="null"/> to use <see cref="Credential"/>.
    /// </param>
    /// <param name="clientOptions">Optional <see cref="BlobClientOptions"/>.</param>
    public EventHubMessagingOptions WithCheckpointStorage(
        Uri serviceUri,
        TokenCredential? credential = null,
        BlobClientOptions? clientOptions = null)
    {
        CheckpointStorage = new CheckpointStorageOptions
        {
            ServiceUri = serviceUri,
            Credential = credential,
            ClientOptions = clientOptions,
        };
        return this;
    }

    /// <summary>
    /// Stores checkpoints in the Blob Storage account identified by <paramref name="connectionString"/>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="clientOptions">Optional client options.</param>
    public EventHubMessagingOptions WithCheckpointStorage(
        string connectionString,
        BlobClientOptions? clientOptions = null)
    {
        CheckpointStorage = new CheckpointStorageOptions
        {
            ConnectionString = connectionString,
            ClientOptions = clientOptions,
        };
        return this;
    }
}