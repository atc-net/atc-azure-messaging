namespace Atc.Azure.Messaging;

/// <summary>
/// The exception passed to <see cref="IProcessErrorHandler"/> when a received payload
/// could not be deserialized into the expected message type.
/// </summary>
/// <remarks>
/// Transports treat these messages as poison: Service Bus dead-letters them, and
/// Storage Queue moves them to the poison queue, since retrying will not help.
/// </remarks>
public sealed class MessageDeserializationException : Exception
{
    public MessageDeserializationException()
    {
    }

    public MessageDeserializationException(string message)
        : base(message)
    {
    }

    public MessageDeserializationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageDeserializationException(
        Type messageType,
        string? messageId,
        Exception? innerException)
        : base(CreateMessage(messageType, messageId), innerException)
    {
        MessageType = messageType;
        MessageId = messageId;
    }

    public Type? MessageType { get; }

    public string? MessageId { get; }

    private static string CreateMessage(
        Type messageType,
        string? messageId)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return $"Message '{messageId}' could not be deserialized into {messageType.Name}.";
    }
}