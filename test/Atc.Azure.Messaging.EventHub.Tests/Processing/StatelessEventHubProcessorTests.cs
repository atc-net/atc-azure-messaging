namespace Atc.Azure.Messaging.EventHub.Tests.Processing;

public sealed class StatelessEventHubProcessorTests
{
    private readonly EventHubConsumerClient client = Substitute.For<EventHubConsumerClient>();

    [Fact]
    public async Task StartProcessingAsync_Should_Read_Every_Partition()
    {
        // Arrange
        var processor = new TestProcessor();
        client.GetPartitionIdsAsync(Arg.Any<CancellationToken>()).Returns(["0", "1"]);
        SetupReads("0", _ => [EventHubTestData.CreateEvent(new TestMessage("a", "tenant"))]);
        SetupReads("1", _ => [EventHubTestData.CreateEvent(new TestMessage("b", "tenant"))]);
        await using var sut = CreateProcessor(processor);

        // Act
        await sut.StartProcessingAsync(CancellationToken.None);
        await Wait.UntilAsync(() => processor.Received.Count == 2);

        // Assert
        sut.IsRunning.Should().BeTrue();
        processor.Received.Select(r => (r.Message.Id, ((EventHubMetadata)r.Metadata).PartitionId))
            .Should().BeEquivalentTo([("a", "0"), ("b", "1")]);
    }

    [Fact]
    public async Task StartProcessingAsync_Should_Start_From_The_Configured_Position()
    {
        // Arrange
        var processor = new TestProcessor();
        client.GetPartitionIdsAsync(Arg.Any<CancellationToken>()).Returns(["0"]);
        SetupReads("0", _ => []);
        await using var sut = CreateProcessor(processor, EventPosition.Earliest);

        // Act
        await sut.StartProcessingAsync(CancellationToken.None);
        await Wait.UntilAsync(() => client.ReceivedCalls().Count() == 2);

        // Assert
        client.Received(1).ReadEventsFromPartitionAsync("0", EventPosition.Earliest, Arg.Any<ReadEventOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopProcessingAsync_Should_Stop_Reading()
    {
        // Arrange
        client.GetPartitionIdsAsync(Arg.Any<CancellationToken>()).Returns(["0"]);
        SetupReads("0", _ => []);
        await using var sut = CreateProcessor(new TestProcessor());
        await sut.StartProcessingAsync(CancellationToken.None);
        await Wait.UntilAsync(() => client.ReceivedCalls().Count() == 2);

        // Act
        await sut.StopProcessingAsync(CancellationToken.None);

        // Assert
        sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Reading_Should_Resume_After_The_Last_Event_When_It_Fails()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var failure = new InvalidOperationException("Reading failed.");
        client.GetPartitionIdsAsync(Arg.Any<CancellationToken>()).Returns(["0"]);
        var reads = 0;
        SetupReads(
            "0",
            _ => Interlocked.Increment(ref reads) == 1
                ? [EventHubTestData.CreateEvent(new TestMessage("1", "tenant"), sequenceNumber: 7)]
                : [],
            readNumber => readNumber == 1 ? failure : null);
        await using var sut = CreateProcessor(processor);

        // Act
        await sut.StartProcessingAsync(CancellationToken.None);
        await Wait.UntilAsync(() => Volatile.Read(ref reads) == 2);

        // Assert
        processor.Errors.Should().Contain(e => e.Exception == failure && e.Metadata == null);
        client.Received(1).ReadEventsFromPartitionAsync(
            "0",
            EventPosition.FromSequenceNumber(7, isInclusive: false),
            Arg.Any<ReadEventOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartProcessingAsync_Should_Retry_When_Partitions_Cannot_Be_Listed()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var failure = new TimeoutException();
        client.GetPartitionIdsAsync(Arg.Any<CancellationToken>()).Returns(
            _ => throw failure,
            _ => Task.FromResult<string[]>(["0"]));
        SetupReads("0", _ => []);
        await using var sut = CreateProcessor(processor);

        // Act
        await sut.StartProcessingAsync(CancellationToken.None);
        await Wait.UntilAsync(() => client.ReceivedCalls().Count() == 3);

        // Assert
        processor.Errors.Should().ContainSingle().Which.Exception.Should().BeSameAs(failure);
    }

    private static async IAsyncEnumerable<PartitionEvent> ReadAsync(
        string partitionId,
        IEnumerable<EventData> events,
        Exception? failure,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var eventData in events)
        {
            yield return EventHubTestData.CreatePartitionEvent(partitionId, eventData);
        }

        if (failure is not null)
        {
            throw failure;
        }

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private StatelessEventHubProcessor<TestMessage, TProcessor> CreateProcessor<TProcessor>(
        TProcessor processor,
        EventPosition? startingPosition = null)
        where TProcessor : class, IMessageProcessor<TestMessage>
        => new(
            client,
            TestDispatcher.Create<TProcessor, EventHubMetadata>(processor),
            readOptions: null,
            startingPosition ?? EventPosition.Latest,
            TimeProvider.System)
        {
            RetryDelay = TimeSpan.Zero,
        };

    private void SetupReads(
        string partitionId,
        Func<EventPosition, IEnumerable<EventData>> events,
        Func<int, Exception?>? failure = null)
    {
        var readNumber = 0;
        client
            .ReadEventsFromPartitionAsync(partitionId, Arg.Any<EventPosition>(), Arg.Any<ReadEventOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => ReadAsync(
                partitionId,
                events(call.ArgAt<EventPosition>(1)),
                failure?.Invoke(Interlocked.Increment(ref readNumber)),
                call.ArgAt<CancellationToken>(3)));
    }
}