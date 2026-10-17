namespace Atc.Azure.Messaging.ServiceBus.Processing.Internal;

/// <summary>
/// Hosts a <see cref="ServiceBusSessionProcessor"/> for a session enabled queue or subscription.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Thin adapter over ServiceBusSessionProcessor, which cannot raise its events in tests.")]
internal sealed class ServiceBusSessionProcessorService<TMessage, TProcessor>
    : IMessageProcessorService<TProcessor>, IHostedService, IAsyncDisposable
    where TProcessor : class, IMessageProcessor<TMessage>
{
    private readonly ServiceBusSessionProcessor processor;
    private readonly ServiceBusMessageHandler<TMessage, TProcessor> handler;
    private readonly Lazy<TProcessor> processorInstance;
    private bool handlersAttached;

    public ServiceBusSessionProcessorService(
        ServiceBusSessionProcessor processor,
        ServiceBusMessageHandler<TMessage, TProcessor> handler,
        Func<TProcessor> processorFactory)
    {
        this.processor = processor;
        this.handler = handler;
        processorInstance = new Lazy<TProcessor>(processorFactory);
    }

    public TProcessor Processor => processorInstance.Value;

    public bool IsRunning => processor.IsProcessing;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!handlersAttached)
        {
            processor.ProcessMessageAsync += OnProcessMessageAsync;
            processor.ProcessErrorAsync += handler.HandleErrorAsync;
            handlersAttached = true;
        }

        return processor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => processor.StopProcessingAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => processor.DisposeAsync();

    private Task OnProcessMessageAsync(ProcessSessionMessageEventArgs args)
        => handler.HandleAsync(args.Message, new SessionMessageSettlement(args), args.CancellationToken);
}