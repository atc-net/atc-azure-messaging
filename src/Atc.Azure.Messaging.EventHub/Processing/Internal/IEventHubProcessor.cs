namespace Atc.Azure.Messaging.EventHub.Processing.Internal;

internal interface IEventHubProcessor
{
    bool IsRunning { get; }

    Task StartProcessingAsync(CancellationToken cancellationToken = default);

    Task StopProcessingAsync(CancellationToken cancellationToken = default);
}