namespace Atc.Azure.Messaging.Serialization;

public interface IMessagePayloadSerializer
{
    string Serialize<T>(T value);
}