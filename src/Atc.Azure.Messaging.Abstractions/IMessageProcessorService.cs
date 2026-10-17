namespace Atc.Azure.Messaging;

/// <summary>
/// Controls the background service that feeds messages to <typeparamref name="TProcessor"/>.
/// </summary>
/// <typeparam name="TProcessor">The type of message processor.</typeparam>
/// <remarks>
/// The service is registered as a hosted service and starts with the application.
/// Use this interface to stop and restart processing at runtime.
/// </remarks>
public interface IMessageProcessorService<out TProcessor>
{
    /// <summary>
    /// Gets the processor instance messages are delivered to.
    /// </summary>
    TProcessor Processor { get; }

    /// <summary>
    /// Gets a value indicating whether messages are currently being processed.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts processing messages.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops processing messages.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}