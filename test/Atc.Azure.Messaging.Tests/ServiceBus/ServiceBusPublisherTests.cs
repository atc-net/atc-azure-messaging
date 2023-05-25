namespace Atc.Azure.Messaging.Tests.ServiceBus;

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
    internal async Task Should_Schedule_Message_On_ServiceBusSender(
        [Frozen] IServiceBusSenderProvider provider,
        ServiceBusPublisher sut,
        [Substitute] ServiceBusSender sender,
        long expectedSequenceNumber,
        string topicName,
        object messageBody,
        DateTimeOffset scheduleTime,
        IDictionary<string, string> properties,
        TimeSpan timeToLive,
        string sessionId,
        CancellationToken cancellationToken)
    {
        provider
            .GetSender(topicName)
            .Returns(sender);

        sender
            .ScheduleMessageAsync(default!, default, default)
            .ReturnsForAnyArgs(expectedSequenceNumber);

        var actualSequenceNumber = await sut.SchedulePublishAsync(
            topicName,
            messageBody,
            scheduleTime,
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        _ = sender
            .Received(1)
            .ScheduleMessageAsync(
                Arg.Any<ServiceBusMessage>(),
                scheduleTime,
                cancellationToken);

        actualSequenceNumber
            .Should()
            .Be(expectedSequenceNumber);

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
    internal async Task Should_Cancel_Scheduled_Message_On_ServiceBusSender(
        [Frozen] IServiceBusSenderProvider provider,
        ServiceBusPublisher sut,
        [Substitute] ServiceBusSender sender,
        long sequenceNumber,
        string topicName,
        CancellationToken cancellationToken)
    {
        provider
            .GetSender(topicName)
            .Returns(sender);

        await sut.CancelScheduledPublishAsync(
            topicName,
            sequenceNumber,
            cancellationToken);

        _ = sender
            .Received(1)
            .CancelScheduledMessageAsync(
                sequenceNumber,
                cancellationToken);
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

    [Theory, AutoNSubstituteData]
    internal async Task Should_Get_ServiceBusSender_For_Topic_On_Batch(
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
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, new List<ServiceBusMessage>());

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(messageBatch);

        await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        _ = provider
            .Received(1)
            .GetSender(topicName);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Create_MessageBatch(
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
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, new List<ServiceBusMessage>());

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(messageBatch);

        await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        _ = await sender
            .Received(1)
            .CreateMessageBatchAsync(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Send_Message_On_ServiceBusSender_On_Message_Batch(
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
        var batchList = new List<ServiceBusMessage>();
        var messageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, batchList);

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(messageBatch);

        await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        var sendMessageBatch = sender
            .ReceivedCallWithArgument<ServiceBusMessageBatch>();

        sendMessageBatch.Count.Should().Be(1);
        batchList.Count.Should().Be(1);

        batchList[0].MessageId
            .Should()
            .NotBeNullOrEmpty();
        batchList[0].Body
            .ToString()
            .Should()
            .BeEquivalentTo(JsonSerializer.Serialize(messageBody));
        batchList[0].ApplicationProperties
            .Should()
            .BeEquivalentTo(properties);
        batchList[0].TimeToLive
           .Should()
           .Be(timeToLive);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Create_New_MessageBatch_When_First_Batch_Is_Full(
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
        var firstBatchList = new List<ServiceBusMessage>();
        var secondBatchList = new List<ServiceBusMessage>();
        var firstMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, firstBatchList, tryAddCallback: _ => false);
        var secondMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, secondBatchList);

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(firstMessageBatch, secondMessageBatch);

        await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        _ = await sender
            .Received(2)
            .CreateMessageBatchAsync(cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    internal async Task Should_Send_Multiple_Batches_If_When_Messages_Exceeds_Single_Batch(
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
        var firstBatchList = new List<ServiceBusMessage>();
        var secondBatchList = new List<ServiceBusMessage>();
        var firstMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, firstBatchList, tryAddCallback: _ => false);
        var secondMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, secondBatchList);

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(firstMessageBatch, secondMessageBatch);

        await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        _ = sender
            .Received(1)
            .SendMessagesAsync(firstMessageBatch, cancellationToken);

        _ = sender
            .Received(1)
            .SendMessagesAsync(secondMessageBatch, cancellationToken);

        firstBatchList.Should().BeEmpty();
        secondBatchList.Count.Should().Be(1);

        secondBatchList[0].MessageId
            .Should()
            .NotBeNullOrEmpty();
        secondBatchList[0].Body
            .ToString()
            .Should()
            .BeEquivalentTo(JsonSerializer.Serialize(messageBody));
        secondBatchList[0].ApplicationProperties
            .Should()
            .BeEquivalentTo(properties);
        secondBatchList[0].TimeToLive
           .Should()
           .Be(timeToLive);
    }

    [Theory, AutoNSubstituteData]
    internal Task Should_Throw_If_Message_Is_Too_Large_To_Fit_In_New_Batch(
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
        var firstBatchList = new List<ServiceBusMessage>();
        var secondBatchList = new List<ServiceBusMessage>();
        var firstMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, firstBatchList, tryAddCallback: _ => false);
        var secondMessageBatch = ServiceBusModelFactory.ServiceBusMessageBatch(265000, secondBatchList, tryAddCallback: _ => false);

        provider
            .GetSender(default!)
            .ReturnsForAnyArgs(sender);

        sender
            .CreateMessageBatchAsync(default!)
            .ReturnsForAnyArgs(firstMessageBatch, secondMessageBatch);

        var act = async () => await sut.PublishAsync(
            topicName,
            new object[] { messageBody },
            sessionId,
            properties,
            timeToLive,
            cancellationToken);

        return act.Should().ThrowAsync<InvalidOperationException>();
    }
}