using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.Tests.EventHub;

public class EventHubPublisherTests
{
    [Theory, AutoNSubstituteData]
    internal async Task PublishAsync_Calls_Client(
        [Frozen, Substitute] EventHubProducerClient client,
        EventHubPublisher sut,
        object messageBody,
        IDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        await sut.PublishAsync(
            messageBody,
            properties,
            cancellationToken);

        _ = client
            .Received(1)
            .SendAsync(
                Arg.Any<EventData[]>(),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task PublishAsync_Calls_Client_With_Correct_MessageBody(
        [Frozen, Substitute] EventHubProducerClient client,
        [Frozen] IMessagePayloadSerializer serializer,
        EventHubPublisher sut,
        object messageBody,
        string serializedMessage,
        IDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        serializer
            .Serialize(messageBody)
            .ReturnsForAnyArgs(serializedMessage);
        await sut.PublishAsync(
            messageBody,
            properties,
            cancellationToken);

        var data = client
            .ReceivedCallWithArgument<EventData[]>()
            .Single();

        data.Body.ToArray()
            .Should()
            .BeEquivalentTo(
                Encoding.UTF8.GetBytes(
                    serializedMessage));
    }

    [Theory, AutoNSubstituteData]
    internal async Task PublishAsync_Calls_Client_With_Correct_Serialized_MessageBody(
        [Frozen, Substitute] EventHubProducerClient client,
        EventHubPublisher sut,
        string serializedMessage,
        IDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        await sut.PublishAsync(
            serializedMessage,
            properties,
            cancellationToken);

        var data = client
            .ReceivedCallWithArgument<EventData[]>()
            .Single();

        data.Body.ToArray()
            .Should()
            .BeEquivalentTo(
                Encoding.UTF8.GetBytes(
                    serializedMessage));
    }

    [Theory, AutoNSubstituteData]
    internal async Task PublishAsync_Calls_Client_With_Correct_Properties(
        [Frozen, Substitute] EventHubProducerClient client,
        EventHubPublisher sut,
        object messageBody,
        IDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        await sut.PublishAsync(
            messageBody,
            properties,
            cancellationToken);

        var data = client
            .ReceivedCallWithArgument<EventData[]>()
            .Single();

        data.Properties
            .Should()
            .BeEquivalentTo(properties);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Disposes_EventHubProducerClient(
        [Frozen, Substitute] EventHubProducerClient client,
        EventHubPublisher sut)
    {
        await sut.DisposeAsync();
        await client.Received(1).DisposeAsync();
    }
}