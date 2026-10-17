namespace Atc.Azure.Messaging.EventHub.Processing.Internal;

/// <summary>
/// Reads all partitions of an Event Hub without checkpointing. Every instance receives every event.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
internal sealed class StatelessEventHubProcessor<TMessage, TProcessor> : IEventHubProcessor, IAsyncDisposable
    where TProcessor : class, IMessageProcessor<TMessage>
{
    private readonly EventHubConsumerClient client;
    private readonly MessageDispatcher<TMessage, TProcessor, EventHubMetadata> dispatcher;
    private readonly ReadEventOptions? readOptions;
    private readonly EventPosition startingPosition;
    private readonly TimeProvider timeProvider;
    private CancellationTokenSource? stoppingSource;
    private Task? executeTask;

    public StatelessEventHubProcessor(
        EventHubConsumerClient client,
        MessageDispatcher<TMessage, TProcessor, EventHubMetadata> dispatcher,
        ReadEventOptions? readOptions,
        EventPosition startingPosition,
        TimeProvider timeProvider)
    {
        this.client = client;
        this.dispatcher = dispatcher;
        this.readOptions = readOptions;
        this.startingPosition = startingPosition;
        this.timeProvider = timeProvider;
    }

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the delay before reading is retried after a failure.
    /// </summary>
    internal TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    public Task StartProcessingAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        IsRunning = true;
        stoppingSource = new CancellationTokenSource();
        executeTask = ExecuteAsync(stoppingSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopProcessingAsync(
        CancellationToken cancellationToken = default)
    {
        if (stoppingSource is null || executeTask is null)
        {
            return;
        }

        await stoppingSource
            .CancelAsync()
            .ConfigureAwait(false);

        try
        {
            await executeTask
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            stoppingSource.Dispose();
            stoppingSource = null;
            executeTask = null;
            IsRunning = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopProcessingAsync(CancellationToken.None)
            .ConfigureAwait(false);

        await client
            .DisposeAsync()
            .ConfigureAwait(false);
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let StartProcessingAsync return before any I/O happens.
        await Task.Yield();

        try
        {
            var partitionIds = await GetPartitionIdsAsync(stoppingToken).ConfigureAwait(false);

            await Task
                .WhenAll(partitionIds.Select(id => ProcessPartitionAsync(id, stoppingToken)))
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<string[]> GetPartitionIdsAsync(
        CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                return await client
                    .GetPartitionIdsAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                await dispatcher
                    .ReportErrorAsync(ex, metadata: null, stoppingToken)
                    .ConfigureAwait(false);

                await Task
                    .Delay(RetryDelay, timeProvider, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reading must survive transient failures; errors are reported to the processor.")]
    private async Task ProcessPartitionAsync(
        string partitionId,
        CancellationToken stoppingToken)
    {
        var position = startingPosition;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var events = client.ReadEventsFromPartitionAsync(partitionId, position, readOptions, stoppingToken);
                await foreach (var partitionEvent in events.ConfigureAwait(false))
                {
                    if (partitionEvent.Data is not { } eventData)
                    {
                        // MaximumWaitTime elapsed without any events.
                        continue;
                    }

                    await dispatcher
                        .DispatchAsync(eventData.EventBody, EventHubMetadata.Create(eventData, partitionId), stoppingToken)
                        .ConfigureAwait(false);

                    position = EventPosition.FromSequenceNumber(eventData.SequenceNumber, isInclusive: false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Transient failures (network, throttling) are reported, then reading resumes after the last event.
                await dispatcher
                    .ReportErrorAsync(ex, metadata: null, stoppingToken)
                    .ConfigureAwait(false);

                await Task
                    .Delay(RetryDelay, timeProvider, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
    }
}