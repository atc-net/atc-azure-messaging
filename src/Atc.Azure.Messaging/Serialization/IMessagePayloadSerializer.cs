namespace Atc.Azure.Messaging.Serialization;

public interface IMessagePayloadSerializer
{
    string Serialize<T>(T value);

    T? Deserialize<T>(string json);
}