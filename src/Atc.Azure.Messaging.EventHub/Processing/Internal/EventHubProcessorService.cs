namespace Atc.Azure.Messaging.EventHub.Processing.Internal;

internal sealed class EventHubProcessorService<TProcessor> : IMessageProcessorService<TProcessor>, IHostedService
{
    private readonly IEventHubProcessor processor;
    private readonly Lazy<TProcessor> processorInstance;

    public EventHubProcessorService(
        IEventHubProcessor processor,
        Func<TProcessor> processorFactory)
    {
        this.processor = processor;
        processorInstance = new Lazy<TProcessor>(processorFactory);
    }

    public TProcessor Processor => processorInstance.Value;

    public bool IsRunning => processor.IsRunning;

    public Task StartAsync(CancellationToken cancellationToken)
        => processor.StartProcessingAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => processor.StopProcessingAsync(cancellationToken);
}