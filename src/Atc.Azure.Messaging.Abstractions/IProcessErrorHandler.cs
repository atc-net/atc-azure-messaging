namespace Atc.Azure.Messaging;

/// <summary>
/// Implemented by a message processor that wants to handle processing errors itself,
/// instead of having them logged as warnings.
/// </summary>
public interface IProcessErrorHandler
{
    /// <summary>
    /// Called when processing a message failed, or when the transport reported an error.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="metadata">
    /// Metadata about the message being processed, or <see langword="null"/>
    /// when the error is not related to a specific message.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ProcessErrorAsync(
        Exception exception,
        MessageMetadata? metadata,
        CancellationToken cancellationToken);
}