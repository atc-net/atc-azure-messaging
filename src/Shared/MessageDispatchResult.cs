namespace Atc.Azure.Messaging.Internal;

internal enum MessageDispatchResult
{
    /// <summary>
    /// The processor handled the message.
    /// </summary>
    Processed,

    /// <summary>
    /// A filter rejected the message, so it was not handed to the processor.
    /// </summary>
    Filtered,

    /// <summary>
    /// The payload could not be deserialized; retrying will not help.
    /// </summary>
    DeserializationFailed,

    /// <summary>
    /// The processor threw an exception.
    /// </summary>
    ProcessingFailed,
}