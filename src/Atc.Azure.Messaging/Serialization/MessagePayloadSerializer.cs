namespace Atc.Azure.Messaging.Serialization;

public class MessagePayloadSerializer : IMessagePayloadSerializer
{
    private readonly JsonSerializerOptions options;

    public MessagePayloadSerializer(JsonSerializerOptions options)
    {
        this.options = options;
    }

    public string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, options);
}