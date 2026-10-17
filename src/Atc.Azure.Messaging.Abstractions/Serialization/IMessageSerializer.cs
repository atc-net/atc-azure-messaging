namespace Atc.Azure.Messaging.Serialization;

/// <summary>
/// Converts messages to and from the payload sent on the wire.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Gets the content type written on published messages, e.g. <c>application/json</c>.
    /// </summary>
    string ContentType { get; }

    BinaryData Serialize<TMessage>(TMessage message);

    TMessage? Deserialize<TMessage>(BinaryData payload);
}