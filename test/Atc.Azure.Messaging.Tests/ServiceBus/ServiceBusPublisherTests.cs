using System.Text.Json;
using Atc.Azure.Messaging.ServiceBus;
using AutoFixture.AutoNSubstitute;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.Tests.ServiceBus
{
    public class ServiceBusPublisherTests
    {
        [Theory, AutoNSubstituteData]
        internal async Task Should_Get_ServiceBusSender_For_Topic(
            [Frozen] IServiceBusSenderProvider provider,
            ServiceBusPublisher sut,
            [Substitute] ServiceBusSender sender,
            string topicName,
            object messageBody,
            IDictionary<string, string> properties,
            TimeSpan timeToLive,
            string sessionId,
            CancellationToken cancellationToken)
        {
            provider
                .GetSender(default!)
                .ReturnsForAnyArgs(sender);

            await sut.PublishAsync(
                topicName,
                messageBody,
                sessionId,
                properties,
                timeToLive,
                cancellationToken);

            _ = provider
                .Received(1)
                .GetSender(topicName);
        }

        [Theory, AutoNSubstituteData]
        internal async Task Should_Send_Message_On_ServiceBusSender(
            [Frozen] IServiceBusSenderProvider provider,
            ServiceBusPublisher sut,
            [Substitute] ServiceBusSender sender,
            string topicName,
            object messageBody,
            IDictionary<string, string> properties,
            TimeSpan timeToLive,
            string sessionId,
            CancellationToken cancellationToken)
        {
            provider
                .GetSender(topicName)
                .Returns(sender);

            await sut.PublishAsync(
                topicName,
                messageBody,
                sessionId,
                properties,
                timeToLive,
                cancellationToken);

            _ = sender
                .Received(1)
                .SendMessageAsync(
                    Arg.Any<ServiceBusMessage>(),
                    cancellationToken);

            var message = sender
                .ReceivedCallWithArgument<ServiceBusMessage>();

            message.MessageId
                .Should()
                .NotBeNullOrEmpty();
            message.Body
                .ToString()
                .Should()
                .BeEquivalentTo(JsonSerializer.Serialize(messageBody));
            message.ApplicationProperties
                .Should()
                .BeEquivalentTo(properties);
            message.TimeToLive
               .Should()
               .Be(timeToLive);
        }

        [Theory, AutoNSubstituteData]
        internal async Task Should_Handle_Default_Parameters(
            [Frozen] IServiceBusSenderProvider provider,
            ServiceBusPublisher sut,
            [Substitute] ServiceBusSender sender,
            string topicName,
            object messageBody,
            string sessionId)
        {
            provider
                .GetSender(topicName)
                .Returns(sender);

            await sut.PublishAsync(
                topicName,
                messageBody,
                sessionId);

            _ = sender
                .Received(1)
                .SendMessageAsync(
                    Arg.Any<ServiceBusMessage>(),
                    CancellationToken.None);

            var message = sender
                .ReceivedCallWithArgument<ServiceBusMessage>();

            message.MessageId
                .Should()
                .NotBeNullOrEmpty();
            message.SessionId
                .Should()
                .NotBeNullOrEmpty();
            message.Body
                .ToString()
                .Should()
                .BeEquivalentTo(JsonSerializer.Serialize(messageBody));
            message.ApplicationProperties
                .Should()
                .BeEmpty();
            message.TimeToLive
               .Should()
               .Be(TimeSpan.MaxValue);
        }
    }
}