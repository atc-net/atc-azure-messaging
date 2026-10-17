namespace Atc.Azure.Messaging.EventHub.Tests.Processing;

public sealed class CheckpointedEventHubProcessorTests
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=a2V5";

    private readonly List<EventData> checkpoints = [];

    [Fact]
    public async Task ProcessBatchAsync_Should_Process_Events_And_Checkpoint_The_Last()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateProcessor(processor);
        EventData[] events =
        [
            EventHubTestData.CreateEvent(new TestMessage("1", "tenant"), sequenceNumber: 1),
            EventHubTestData.CreateEvent(new TestMessage("2", "tenant"), sequenceNumber: 2),
        ];

        // Act
        await sut.ProcessBatchAsync(events, "0", Checkpoint, CancellationToken.None);

        // Assert
        processor.Received.Select(r => r.Message.Id).Should().Equal("1", "2");
        processor.Received.Select(r => r.Metadata).Should().AllBeOfType<EventHubMetadata>()
            .Which.Should().AllSatisfy(m => m.PartitionId.Should().Be("0"));
        checkpoints.Should().ContainSingle().Which.Should().BeSameAs(events[1]);
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_Checkpoint_Past_Failed_And_Filtered_Events()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateProcessor(processor, m => m.MessageId != "skip");
        EventData[] events =
        [
            EventHubTestData.CreateEvent(new TestMessage(TestProcessor.FailingMessageId, "tenant"), sequenceNumber: 1),
            EventHubTestData.CreateEvent(new TestMessage("skip", "tenant"), sequenceNumber: 2),
        ];

        // Act
        await sut.ProcessBatchAsync(events, "0", Checkpoint, CancellationToken.None);

        // Assert
        processor.Received.Should().BeEmpty();
        checkpoints.Should().ContainSingle().Which.Should().BeSameAs(events[1]);
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_Not_Checkpoint_An_Empty_Batch()
    {
        // Arrange
        var sut = CreateProcessor(new TestProcessor());

        // Act
        await sut.ProcessBatchAsync([], "0", Checkpoint, CancellationToken.None);

        // Assert
        checkpoints.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_Should_Not_Checkpoint_When_Stopping()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var processor = new TestProcessor
        {
            OnProcess = async (_, ct) =>
            {
                await cancellation.CancelAsync();
                ct.ThrowIfCancellationRequested();
            },
        };
        var sut = CreateProcessor(processor);

        // Act
        await sut.ProcessBatchAsync(
            [EventHubTestData.CreateEvent(new TestMessage("1", "tenant"))],
            "0",
            Checkpoint,
            cancellation.Token);

        // Assert
        checkpoints.Should().BeEmpty();
    }

    private static CheckpointedEventHubProcessor<TestMessage, TestProcessor> CreateProcessor(
        TestProcessor processor,
        params Func<EventHubMetadata, bool>[] filters)
        => new(
            TestDispatcher.Create(processor, filters),
            new BlobContainerClient(new Uri("https://account.blob.core.windows.net/checkpoints")),
            createContainerIfNotExists: false,
            maxBatchSize: 100,
            consumerGroup: "$Default",
            connectionString: ConnectionString,
            eventHubName: "hub",
            processorOptions: null);

    private Task Checkpoint(EventData eventData)
    {
        checkpoints.Add(eventData);
        return Task.CompletedTask;
    }
}