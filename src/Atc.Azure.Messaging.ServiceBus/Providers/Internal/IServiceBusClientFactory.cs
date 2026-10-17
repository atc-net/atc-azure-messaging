namespace Atc.Azure.Messaging.ServiceBus.Providers.Internal;

internal interface IServiceBusClientFactory
{
    /// <summary>
    /// Gets the validated options of a connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <returns>The options.</returns>
    ServiceBusMessagingOptions GetOptions(string? connectionName);

    /// <summary>
    /// Gets the cached client of a connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <returns>The client.</returns>
    ServiceBusClient GetClient(string? connectionName);

    /// <summary>
    /// Gets a cached sender, shared by all publishers of the same entity on the connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <param name="topicOrQueueName">The name of the topic or queue.</param>
    /// <param name="senderOptions">The options used when creating the sender.</param>
    /// <returns>The sender.</returns>
    ServiceBusSender GetSender(
        string? connectionName,
        string topicOrQueueName,
        ServiceBusSenderOptions? senderOptions);
}