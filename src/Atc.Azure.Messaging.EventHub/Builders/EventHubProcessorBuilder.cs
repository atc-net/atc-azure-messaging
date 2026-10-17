namespace Atc.Azure.Messaging.EventHub.Builders;

/// <summary>
/// Configures a checkpointed Event Hub processor.
/// </summary>
public class EventHubProcessorBuilder
{
    internal const int DefaultMaxBatchSize = 100;

    internal EventHubProcessorBuilder(string eventHubName)
    {
        ContainerName = eventHubName;
    }

    internal List<Func<EventHubMetadata, bool>> Filters { get; } = [];

    internal EventProcessorOptions? ProcessorOptions { get; private set; }

    internal string ContainerName { get; private set; }

    internal bool CreateContainerIfNotExists { get; private set; } = true;

    internal int MaxBatchSize { get; private set; } = DefaultMaxBatchSize;

    /// <summary>
    /// Only hands events matching <paramref name="predicate"/> to the processor. Other events are skipped
    /// and checkpointed. Multiple filters must all match.
    /// </summary>
    /// <param name="predicate">The condition a message must match to be processed.</param>
    public EventHubProcessorBuilder WithFilter(
        Func<EventHubMetadata, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    public EventHubProcessorBuilder WithProcessorOptions(
        EventProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ProcessorOptions = options;
        return this;
    }

    /// <summary>
    /// Sets the blob container used for checkpoints. Defaults to the Event Hub name.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="createIfNotExists">Whether to create the resource when processing starts, if it does not exist.</param>
    public EventHubProcessorBuilder WithCheckpointContainer(
        string containerName,
        bool createIfNotExists = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ContainerName = containerName;
        CreateContainerIfNotExists = createIfNotExists;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of events read per batch. A checkpoint is written after each batch.
    /// Defaults to 100.
    /// </summary>
    /// <param name="maxBatchSize">The maximum number of events per batch.</param>
    public EventHubProcessorBuilder WithMaxBatchSize(int maxBatchSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBatchSize, 1);
        MaxBatchSize = maxBatchSize;
        return this;
    }
}