namespace Atc.Azure.Messaging.StorageQueue.Options;

/// <summary>
/// Options for a Storage Queue connection. A connection is identified by its name,
/// where <see langword="null"/> is the default connection.
/// </summary>
/// <remarks>
/// Set either <see cref="QueueServiceUri"/> and <see cref="Credential"/>, or <see cref="ConnectionString"/>.
/// </remarks>
public class StorageQueueMessagingOptions
{
    /// <summary>
    /// Gets or sets the queue service endpoint, e.g. <c>https://account.queue.core.windows.net</c>.
    /// </summary>
    public Uri? QueueServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used together with <see cref="QueueServiceUri"/>,
    /// e.g. a <c>DefaultAzureCredential</c> or <c>ManagedIdentityCredential</c>.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a connection string for the storage account.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the serializer used for message payloads. Defaults to <see cref="JsonMessageSerializer"/>.
    /// </summary>
    public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

    /// <summary>
    /// Gets or sets options for the queue clients, e.g. <see cref="QueueClientOptions.MessageEncoding"/>
    /// set to <see cref="QueueMessageEncoding.Base64"/> to interoperate with Azure Functions.
    /// </summary>
    public QueueClientOptions? ClientOptions { get; set; }

    /// <summary>
    /// Connects to <paramref name="queueServiceUri"/> with <paramref name="credential"/>.
    /// </summary>
    /// <param name="queueServiceUri">The queue service endpoint, e.g. <c>https://account.queue.core.windows.net</c>.</param>
    /// <param name="credential">The credential used to authenticate.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingOptions WithConnection(
        Uri queueServiceUri,
        TokenCredential credential)
    {
        QueueServiceUri = queueServiceUri;
        Credential = credential;
        return this;
    }

    /// <summary>
    /// Connects with a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingOptions WithConnectionString(
        string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Uses <paramref name="serializer"/> for message payloads.
    /// </summary>
    /// <param name="serializer">The serializer used for message payloads.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingOptions WithSerializer(
        IMessageSerializer serializer)
    {
        Serializer = serializer;
        return this;
    }

    /// <summary>
    /// Uses the default JSON serializer with <paramref name="serializerOptions"/>.
    /// </summary>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used by the default JSON serializer.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingOptions WithJsonSerializerOptions(
        JsonSerializerOptions serializerOptions)
    {
        Serializer = new JsonMessageSerializer(serializerOptions);
        return this;
    }

    /// <summary>
    /// Uses <paramref name="options"/> for the queue clients.
    /// </summary>
    /// <param name="options">The options passed to the client.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingOptions WithClientOptions(
        QueueClientOptions options)
    {
        ClientOptions = options;
        return this;
    }
}