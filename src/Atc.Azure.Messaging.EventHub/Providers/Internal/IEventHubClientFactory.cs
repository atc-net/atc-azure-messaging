namespace Atc.Azure.Messaging.EventHub.Providers.Internal;

internal interface IEventHubClientFactory
{
    /// <summary>
    /// Gets the validated options of a connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    EventHubMessagingOptions GetOptions(string? connectionName);

    /// <summary>
    /// Gets a cached producer client, shared by all publishers of the same Event Hub on the connection.
    /// </summary>
    /// <param name="connectionName">The connection name, or <see langword="null"/> for the default connection.</param>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    EventHubProducerClient GetProducerClient(
        string? connectionName,
        string eventHubName);

    EventHubConsumerClient CreateConsumerClient(
        string? connectionName,
        string eventHubName,
        string consumerGroup);

    BlobContainerClient CreateCheckpointContainerClient(
        string? connectionName,
        string containerName);
}