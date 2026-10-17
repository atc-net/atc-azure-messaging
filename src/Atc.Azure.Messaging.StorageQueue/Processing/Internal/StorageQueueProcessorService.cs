namespace Atc.Azure.Messaging.StorageQueue.Processing.Internal;

/// <summary>
/// Polls a Storage Queue and hands messages to the processor.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
/// <remarks>
/// Processed messages are deleted. Messages that cannot be deserialized, or that failed
/// <see cref="StorageQueueProcessorSettings.MaxDequeueCount"/> times, are moved to the poison queue.
/// Other failed messages become visible again when their visibility timeout expires.
/// </remarks>
internal sealed class StorageQueueProcessorService<TMessage, TProcessor>
    : BackgroundService, IMessageProcessorService<TProcessor>
    where TProcessor : class, IMessageProcessor<TMessage>
{
    private readonly QueueClient queue;
    private readonly QueueClient poisonQueue;
    private readonly MessageDispatcher<TMessage, TProcessor, StorageQueueMetadata> dispatcher;
    private readonly StorageQueueProcessorSettings settings;
    private readonly TimeProvider timeProvider;
    private readonly Lazy<TProcessor> processorInstance;

    public StorageQueueProcessorService(
        QueueClient queue,
        QueueClient poisonQueue,
        MessageDispatcher<TMessage, TProcessor, StorageQueueMetadata> dispatcher,
        StorageQueueProcessorSettings settings,
        TimeProvider timeProvider,
        Func<TProcessor> processorFactory)
    {
        this.queue = queue;
        this.poisonQueue = poisonQueue;
        this.dispatcher = dispatcher;
        this.settings = settings;
        this.timeProvider = timeProvider;
        processorInstance = new Lazy<TProcessor>(processorFactory);
    }

    public TProcessor Processor => processorInstance.Value;

    public bool IsRunning { get; private set; }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base
            .StopAsync(cancellationToken)
            .ConfigureAwait(false);

        IsRunning = false;
    }

    internal async Task<int> ProcessNextBatchAsync(
        CancellationToken cancellationToken)
    {
        var response = await queue
            .ReceiveMessagesAsync(settings.MaxMessages, settings.VisibilityTimeout, cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in response.Value)
        {
            await HandleMessageAsync(message, cancellationToken)
                .ConfigureAwait(false);
        }

        return response.Value.Length;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (settings.CreateQueuesIfNotExist)
            {
                await CreateQueuesAsync(stoppingToken)
                    .ConfigureAwait(false);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await PollAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Polling must survive transient failures; errors are reported to the processor.")]
    private async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            var received = await ProcessNextBatchAsync(stoppingToken)
                .ConfigureAwait(false);

            if (received == 0)
            {
                await Task
                    .Delay(settings.PollingInterval, timeProvider, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            await dispatcher
                .ReportErrorAsync(ex, metadata: null, stoppingToken)
                .ConfigureAwait(false);

            await Task
                .Delay(settings.RetryDelay, timeProvider, stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task CreateQueuesAsync(CancellationToken cancellationToken)
    {
        await queue
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await poisonQueue
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleMessageAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        var metadata = StorageQueueMetadata.Create(message, queue.Name);
        var result = await dispatcher
            .DispatchAsync(message.Body, metadata, cancellationToken)
            .ConfigureAwait(false);

        switch (result)
        {
            case MessageDispatchResult.Processed:
            case MessageDispatchResult.Filtered:
                await queue
                    .DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case MessageDispatchResult.DeserializationFailed:
                await MoveToPoisonQueueAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case MessageDispatchResult.ProcessingFailed when message.DequeueCount >= settings.MaxDequeueCount:
                await MoveToPoisonQueueAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                // Leave the message on the queue; it is received again when the visibility timeout expires.
                break;
        }
    }

    private async Task MoveToPoisonQueueAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        await poisonQueue
            .SendMessageAsync(message.Body, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await queue
            .DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken)
            .ConfigureAwait(false);
    }
}