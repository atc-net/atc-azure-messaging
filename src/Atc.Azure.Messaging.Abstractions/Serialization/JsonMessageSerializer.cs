namespace Atc.Azure.Messaging.Serialization;

/// <summary>
/// The default <see cref="IMessageSerializer"/>, based on <see cref="System.Text.Json"/>.
/// </summary>
public sealed class JsonMessageSerializer : IMessageSerializer
{
    public JsonMessageSerializer()
        : this(DefaultSerializerOptions)
    {
    }

    public JsonMessageSerializer(JsonSerializerOptions serializerOptions)
    {
        ArgumentNullException.ThrowIfNull(serializerOptions);
        SerializerOptions = serializerOptions;
    }

    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions"/>: <see cref="JsonSerializerDefaults.Web"/>,
    /// i.e. camelCase property names and case insensitive deserialization.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = new(JsonSerializerDefaults.Web);

    public JsonSerializerOptions SerializerOptions { get; }

    public string ContentType => "application/json";

    public BinaryData Serialize<TMessage>(TMessage message)
        => BinaryData.FromObjectAsJson(message, SerializerOptions);

    public TMessage? Deserialize<TMessage>(BinaryData payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.ToObjectFromJson<TMessage>(SerializerOptions);
    }
}