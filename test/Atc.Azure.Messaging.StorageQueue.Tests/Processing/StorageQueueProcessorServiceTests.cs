namespace Atc.Azure.Messaging.StorageQueue.Tests.Processing;

public sealed class StorageQueueProcessorServiceTests
{
    private static readonly StorageQueueProcessorSettings Settings = new(
        PollingInterval: TimeSpan.FromSeconds(5),
        MaxMessages: 16,
        VisibilityTimeout: TimeSpan.FromSeconds(30),
        MaxDequeueCount: 3,
        CreateQueuesIfNotExist: false);

    private readonly QueueClient queue = Substitute.For<QueueClient>();
    private readonly QueueClient poisonQueue = Substitute.For<QueueClient>();
    private readonly FakeTimeProvider timeProvider = new();

    public StorageQueueProcessorServiceTests()
    {
        queue.Name.Returns("orders");
        SetupReceive();
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Receive_With_The_Configured_Settings()
    {
        // Arrange
        SetupReceive(StorageQueueTestData.CreateMessage(new TestMessage("1", "tenant")));
        using var sut = CreateService(new TestProcessor());

        // Act
        var received = await sut.ProcessNextBatchAsync(CancellationToken.None);

        // Assert
        received.Should().Be(1);
        await queue.Received(1).ReceiveMessagesAsync(16, TimeSpan.FromSeconds(30), CancellationToken.None);
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Delete_Processed_Messages()
    {
        // Arrange
        var processor = new TestProcessor();
        SetupReceive(StorageQueueTestData.CreateMessage(new TestMessage("1", "tenant")));
        using var sut = CreateService(processor);

        // Act
        await sut.ProcessNextBatchAsync(CancellationToken.None);

        // Assert
        processor.Received.Should().ContainSingle()
            .Which.Metadata.Should().BeOfType<StorageQueueMetadata>()
            .Which.QueueName.Should().Be("orders");
        await queue.Received(1).DeleteMessageAsync("1", "receipt-1", CancellationToken.None);
        poisonQueue.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Move_Unreadable_Messages_To_The_Poison_Queue()
    {
        // Arrange
        var body = BinaryData.FromString("not json");
        SetupReceive(StorageQueueTestData.CreateMessage(body, "1"));
        using var sut = CreateService(new TestProcessor());

        // Act
        await sut.ProcessNextBatchAsync(CancellationToken.None);

        // Assert
        await poisonQueue.Received(1).SendMessageAsync(body, null, null, CancellationToken.None);
        await queue.Received(1).DeleteMessageAsync("1", "receipt-1", CancellationToken.None);
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Leave_Failed_Messages_For_Retry()
    {
        // Arrange
        SetupReceive(StorageQueueTestData.CreateMessage(new TestMessage(TestProcessor.FailingMessageId, "tenant"), dequeueCount: 2));
        using var sut = CreateService(new TestProcessor());

        // Act
        await sut.ProcessNextBatchAsync(CancellationToken.None);

        // Assert
        await queue.DidNotReceiveWithAnyArgs().DeleteMessageAsync(default!, default!, CancellationToken.None);
        poisonQueue.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessNextBatchAsync_Should_Move_Messages_To_The_Poison_Queue_After_The_Last_Attempt()
    {
        // Arrange
        var message = StorageQueueTestData.CreateMessage(new TestMessage(TestProcessor.FailingMessageId, "tenant"), dequeueCount: 3);
        SetupReceive(message);
        using var sut = CreateService(new TestProcessor());

        // Act
        await sut.ProcessNextBatchAsync(CancellationToken.None);

        // Assert
        await poisonQueue.Received(1).SendMessageAsync(message.Body, null, null, CancellationToken.None);
        await queue.Received(1).DeleteMessageAsync(message.MessageId, message.PopReceipt, CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_Should_Create_Queues_And_Poll_Until_Stopped()
    {
        // Arrange
        using var sut = CreateService(new TestProcessor(), Settings with { CreateQueuesIfNotExist = true });

        // Act
        await sut.StartAsync(CancellationToken.None);
        var isRunning = sut.IsRunning;
        await WaitForReceivesAsync(2, Settings.PollingInterval);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        isRunning.Should().BeTrue();
        sut.IsRunning.Should().BeFalse();
        await queue.Received(1).CreateIfNotExistsAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await poisonQueue.Received(1).CreateIfNotExistsAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_Should_Report_Polling_Failures_And_Keep_Polling()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var failure = new RequestFailedException("Unavailable.");
        queue
            .ReceiveMessagesAsync(Arg.Any<int?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<Response<QueueMessage[]>>(failure),
                _ => Task.FromResult(Response.FromValue(Array.Empty<QueueMessage>(), Substitute.For<Response>())));
        using var sut = CreateService(processor);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await WaitForReceivesAsync(2, Settings.RetryDelay);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        processor.Errors.Should().ContainSingle()
            .Which.Should().Be((failure, null));
    }

    private Task WaitForReceivesAsync(
        int count,
        TimeSpan delay)
        => Wait.UntilAsync(() =>
        {
            // The polling loop runs in the background, so time is advanced until it has polled again.
            timeProvider.Advance(delay);
            return queue.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(QueueClient.ReceiveMessagesAsync)) >= count;
        });

    private StorageQueueProcessorService<TestMessage, TProcessor> CreateService<TProcessor>(
        TProcessor processor,
        StorageQueueProcessorSettings? settings = null)
        where TProcessor : class, IMessageProcessor<TestMessage>
        => new(
            queue,
            poisonQueue,
            TestDispatcher.Create<TProcessor, StorageQueueMetadata>(processor),
            settings ?? Settings,
            timeProvider,
            () => processor);

    private void SetupReceive(params QueueMessage[] messages)
        => queue
            .ReceiveMessagesAsync(Arg.Any<int?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(
                Response.FromValue(messages, Substitute.For<Response>()),
                Response.FromValue(Array.Empty<QueueMessage>(), Substitute.For<Response>()));
}