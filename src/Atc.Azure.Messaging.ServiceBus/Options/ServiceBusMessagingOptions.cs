namespace Atc.Azure.Messaging.ServiceBus.Options;

/// <summary>
/// Options for a Service Bus connection. A connection is identified by its name,
/// where <see langword="null"/> is the default connection.
/// </summary>
/// <remarks>
/// Set either <see cref="FullyQualifiedNamespace"/> and <see cref="Credential"/>, or <see cref="ConnectionString"/>.
/// </remarks>
public class ServiceBusMessagingOptions
{
    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace, e.g. <c>my-namespace.servicebus.windows.net</c>.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the credential used together with <see cref="FullyQualifiedNamespace"/>,
    /// e.g. a <c>DefaultAzureCredential</c> or <c>ManagedIdentityCredential</c>.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a connection string for the Service Bus namespace.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the serializer used for message payloads. Defaults to <see cref="JsonMessageSerializer"/>.
    /// </summary>
    public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

    /// <summary>
    /// Gets or sets options for the <see cref="ServiceBusClient"/>, e.g. retry options or the transport type.
    /// </summary>
    public ServiceBusClientOptions? ClientOptions { get; set; }

    /// <summary>
    /// Connects to <paramref name="fullyQualifiedNamespace"/> with <paramref name="credential"/>.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified namespace, e.g. <c>my-namespace.servicebus.windows.net</c>.</param>
    /// <param name="credential">The credential used to authenticate.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingOptions WithConnection(
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        FullyQualifiedNamespace = fullyQualifiedNamespace;
        Credential = credential;
        return this;
    }

    /// <summary>
    /// Connects with a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingOptions WithConnectionString(
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
    public ServiceBusMessagingOptions WithSerializer(
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
    public ServiceBusMessagingOptions WithJsonSerializerOptions(
        JsonSerializerOptions serializerOptions)
    {
        Serializer = new JsonMessageSerializer(serializerOptions);
        return this;
    }

    /// <summary>
    /// Uses <paramref name="options"/> for the <see cref="ServiceBusClient"/>.
    /// </summary>
    /// <param name="options">The options passed to the client.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingOptions WithClientOptions(
        ServiceBusClientOptions options)
    {
        ClientOptions = options;
        return this;
    }
}