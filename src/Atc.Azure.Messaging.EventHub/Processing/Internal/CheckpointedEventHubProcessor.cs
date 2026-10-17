namespace Atc.Azure.Messaging.EventHub.Processing.Internal;

/// <summary>
/// Reads events with load balancing across instances, storing checkpoints and partition ownership in Blob Storage.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
internal sealed class CheckpointedEventHubProcessor<TMessage, TProcessor>
    : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>, IEventHubProcessor
    where TProcessor : class, IMessageProcessor<TMessage>
{
    private readonly MessageDispatcher<TMessage, TProcessor, EventHubMetadata> dispatcher;
    private readonly BlobContainerClient checkpointContainer;
    private readonly bool createContainerIfNotExists;

    public CheckpointedEventHubProcessor(
        MessageDispatcher<TMessage, TProcessor, EventHubMetadata> dispatcher,
        BlobContainerClient checkpointContainer,
        bool createContainerIfNotExists,
        int maxBatchSize,
        string consumerGroup,
        string fullyQualifiedNamespace,
        string eventHubName,
        TokenCredential credential,
        EventProcessorOptions? processorOptions)
        : base(
            new BlobCheckpointStore(checkpointContainer),
            maxBatchSize,
            consumerGroup,
            fullyQualifiedNamespace,
            eventHubName,
            credential,
            processorOptions)
    {
        this.dispatcher = dispatcher;
        this.checkpointContainer = checkpointContainer;
        this.createContainerIfNotExists = createContainerIfNotExists;
    }

    public CheckpointedEventHubProcessor(
        MessageDispatcher<TMessage, TProcessor, EventHubMetadata> dispatcher,
        BlobContainerClient checkpointContainer,
        bool createContainerIfNotExists,
        int maxBatchSize,
        string consumerGroup,
        string connectionString,
        string eventHubName,
        EventProcessorOptions? processorOptions)
        : base(
            new BlobCheckpointStore(checkpointContainer),
            maxBatchSize,
            consumerGroup,
            connectionString,
            eventHubName,
            processorOptions)
    {
        this.dispatcher = dispatcher;
        this.checkpointContainer = checkpointContainer;
        this.createContainerIfNotExists = createContainerIfNotExists;
    }

    public override async Task StartProcessingAsync(
        CancellationToken cancellationToken = default)
    {
        if (createContainerIfNotExists)
        {
            await checkpointContainer
                .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        await base
            .StartProcessingAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    internal async Task ProcessBatchAsync(
        IEnumerable<EventData> events,
        string partitionId,
        Func<EventData, Task> checkpoint,
        CancellationToken cancellationToken)
    {
        EventData? lastEvent = null;
        try
        {
            foreach (var eventData in events)
            {
                await dispatcher
                    .DispatchAsync(eventData.EventBody, EventHubMetadata.Create(eventData, partitionId), cancellationToken)
                    .ConfigureAwait(false);

                lastEvent = eventData;
            }

            if (lastEvent is not null)
            {
                await checkpoint(lastEvent).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Processing stopped, or the partition was claimed by another instance.
            // No checkpoint is written, so the next owner resumes from the last stored checkpoint.
        }
    }

    protected override Task OnProcessingEventBatchAsync(
        IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
        => ProcessBatchAsync(
            events,
            partition.PartitionId,
            lastEvent => UpdateCheckpointAsync(
                partition.PartitionId,
                CheckpointPosition.FromEvent(lastEvent),
                cancellationToken),
            cancellationToken);

    protected override Task OnProcessingErrorAsync(
        Exception exception,
        EventProcessorPartition partition,
        string operationDescription,
        CancellationToken cancellationToken)
        => dispatcher.ReportErrorAsync(exception, metadata: null, cancellationToken);
}