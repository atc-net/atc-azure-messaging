namespace Atc.Azure.Messaging.StorageQueue.Tests.Publishing;

public sealed class StorageQueuePublisherTests
{
    private readonly QueueClient queue = Substitute.For<QueueClient>();

    [Fact]
    public async Task PublishAsync_Should_Send_Serialized_Message()
    {
        // Arrange
        var sut = CreatePublisher();
        var message = new TestMessage("1", "tenant");

        // Act
        await sut.PublishAsync(message, CancellationToken.None);

        // Assert
        await queue.Received(1).SendMessageAsync(
            Arg.Is<BinaryData>(b => b.ToObjectFromJson<TestMessage>(JsonSerializerOptions.Web) == message),
            null,
            null,
            CancellationToken.None);
        await queue.DidNotReceiveWithAnyArgs().CreateIfNotExistsAsync(null, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_Should_Use_The_Default_Time_To_Live()
    {
        // Arrange
        var sut = CreatePublisher(p => p.WithTimeToLive(TimeSpan.FromHours(1)));

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), CancellationToken.None);

        // Assert
        await queue.Received(1).SendMessageAsync(Arg.Any<BinaryData>(), null, TimeSpan.FromHours(1), CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_Should_Let_Options_Override_Defaults()
    {
        // Arrange
        var sut = CreatePublisher(p => p.WithTimeToLive(TimeSpan.FromHours(1)));
        var options = new StorageQueuePublishingOptions
        {
            InitialVisibilityDelay = TimeSpan.FromMinutes(1),
            TimeToLive = TimeSpan.FromMinutes(10),
        };

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), options, CancellationToken.None);

        // Assert
        await queue.Received(1).SendMessageAsync(
            Arg.Any<BinaryData>(),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(10),
            CancellationToken.None);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Send_Every_Message()
    {
        // Arrange
        var sut = CreatePublisher();

        // Act
        await sut.PublishBatchAsync(
            [new TestMessage("1", "tenant"), new TestMessage("2", "tenant"), new TestMessage("3", "tenant")],
            CancellationToken.None);

        // Assert
        await queue.ReceivedWithAnyArgs(3).SendMessageAsync(default(BinaryData)!, default, default, CancellationToken.None);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Throw_For_Null_Messages()
    {
        // Arrange
        var sut = CreatePublisher();

        // Act
        var act = () => sut.PublishBatchAsync([null!], CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().Be("messages");
    }

    [Fact]
    public async Task PublishAsync_Should_Create_The_Queue_Once_When_Configured()
    {
        // Arrange
        var sut = CreatePublisher(p => p.CreateIfNotExists());

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), CancellationToken.None);
        await sut.PublishAsync(new TestMessage("2", "tenant"), CancellationToken.None);

        // Assert
        await queue.Received(1).CreateIfNotExistsAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_Should_Retry_Creating_The_Queue_After_A_Failure()
    {
        // Arrange
        queue
            .CreateIfNotExistsAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<Response>(new RequestFailedException("Unavailable.")),
                _ => Task.FromResult(Substitute.For<Response>()));
        var sut = CreatePublisher(p => p.CreateIfNotExists());

        // Act
        var first = () => sut.PublishAsync(new TestMessage("1", "tenant"), CancellationToken.None);
        await first.Should().ThrowAsync<RequestFailedException>();
        await sut.PublishAsync(new TestMessage("2", "tenant"), CancellationToken.None);

        // Assert
        await queue.Received(2).CreateIfNotExistsAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await queue.ReceivedWithAnyArgs(1).SendMessageAsync(default(BinaryData)!, default, default, CancellationToken.None);
    }

    private StorageQueuePublisher<TestMessage> CreatePublisher(
        Action<StorageQueuePublisherBuilder<TestMessage>>? configure = null)
    {
        var builder = new StorageQueuePublisherBuilder<TestMessage>();
        configure?.Invoke(builder);

        return new StorageQueuePublisher<TestMessage>(queue, new JsonMessageSerializer(), builder);
    }
}