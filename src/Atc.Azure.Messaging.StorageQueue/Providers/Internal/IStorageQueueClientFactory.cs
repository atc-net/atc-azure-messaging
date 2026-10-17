namespace Atc.Azure.Messaging.StorageQueue.Providers.Internal;

internal interface IStorageQueueClientFactory
{
    /// <summary>
    /// Gets the validated options of a connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <returns>The options.</returns>
    StorageQueueMessagingOptions GetOptions(string? connectionName);

    /// <summary>
    /// Gets a client for a queue on the connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <returns>The queue client.</returns>
    QueueClient GetQueueClient(
        string? connectionName,
        string queueName);
}