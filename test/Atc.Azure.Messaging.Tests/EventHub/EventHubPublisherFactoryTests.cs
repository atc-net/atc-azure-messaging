using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.Tests.EventHub;

public class EventHubPublisherFactoryTests
{
    private const string Endpoint = "sb://servicebus.windows.net/";
    private const string ConnectionString =
        $"Endpoint={Endpoint};SharedAccessKeyName=<KeyName>;SharedAccessKey=<KeyValue>;";

    [Theory, AutoNSubstituteData]
    public void Create_Returns_IEventHubPublisher(
        IMessagePayloadSerializer messagePayloadSerializer,
        string eventHubName)
        => new EventHubPublisherFactory(
                new EventHubOptions { ConnectionString = ConnectionString },
                messagePayloadSerializer)
            .Create(eventHubName)
            .Should()
            .BeAssignableTo<IEventHubPublisher>();

    [Theory, AutoNSubstituteData]
    public void Create_Returns_NotNull(
        IMessagePayloadSerializer messagePayloadSerializer,
        string eventHubName)
        => new EventHubPublisherFactory(
                new EventHubOptions { ConnectionString = ConnectionString },
                messagePayloadSerializer)
            .Create(eventHubName)
            .Should()
            .NotBeNull();
}