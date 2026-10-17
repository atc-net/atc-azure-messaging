namespace Atc.Azure.Messaging.EventHub.Tests.Publishing;

public sealed class EventHubPublisherTests
{
    private readonly EventHubProducerClient producer = Substitute.For<EventHubProducerClient>();
    private readonly List<List<EventData>> sentBatches = [];
    private readonly List<CreateBatchOptions> batchOptions = [];
    private readonly Dictionary<EventDataBatch, List<EventData>> batchStores = new(ReferenceEqualityComparer.Instance);

    [Fact]
    public async Task PublishAsync_Should_Send_Serialized_Message()
    {
        // Arrange
        var sut = CreatePublisher();
        var message = new TestMessage("1", "tenant");

        // Act
        await sut.PublishAsync(message, CancellationToken.None);

        // Assert
        var eventData = GetSingleSentEvent();
        eventData.EventBody.ToObjectFromJson<TestMessage>(JsonSerializerOptions.Web).Should().Be(message);
        eventData.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task PublishAsync_Should_Apply_Publisher_Defaults()
    {
        // Arrange
        var sut = CreatePublisher(p => p
            .WithPartitionKey(m => m.TenantId)
            .WithMessageId(m => m.Id)
            .WithCorrelationId(m => $"correlation-{m.Id}")
            .WithProperty(m => m.TenantId)
            .WithProperty("origin", "tests"));

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), CancellationToken.None);

        // Assert
        var eventData = GetSingleSentEvent();
        eventData.MessageId.Should().Be("1");
        eventData.CorrelationId.Should().Be("correlation-1");
        eventData.Properties.Should().Contain("TenantId", "tenant").And.Contain("origin", "tests");
        await producer.Received(1).SendAsync(
            Arg.Any<IEnumerable<EventData>>(),
            Arg.Is<SendEventOptions>(o => o.PartitionKey == "tenant" && o.PartitionId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_Should_Let_Options_Override_Defaults()
    {
        // Arrange
        var sut = CreatePublisher(p => p
            .WithPartitionKey(m => m.TenantId)
            .WithMessageId(m => m.Id)
            .WithProperty("origin", "tests"));
        var options = new PublishingOptions
        {
            MessageId = "custom-id",
            CorrelationId = "custom-correlation",
            ContentType = "application/custom",
            PartitionKey = "custom-key",
            Properties = { ["origin"] = "options" },
        };

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), options, CancellationToken.None);

        // Assert
        var eventData = GetSingleSentEvent();
        eventData.MessageId.Should().Be("custom-id");
        eventData.CorrelationId.Should().Be("custom-correlation");
        eventData.ContentType.Should().Be("application/custom");
        eventData.Properties.Should().Contain("origin", "options");
        await producer.Received(1).SendAsync(
            Arg.Any<IEnumerable<EventData>>(),
            Arg.Is<SendEventOptions>(o => o.PartitionKey == "custom-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_Should_Send_To_Partition_Id_Without_Partition_Key()
    {
        // Arrange
        var sut = CreatePublisher(p => p.WithPartitionKey(m => m.TenantId));

        // Act
        await sut.PublishAsync(
            new TestMessage("1", "tenant"),
            new EventHubPublishingOptions { PartitionId = "3" },
            CancellationToken.None);

        // Assert
        await producer.Received(1).SendAsync(
            Arg.Any<IEnumerable<EventData>>(),
            Arg.Is<SendEventOptions>(o => o.PartitionId == "3" && o.PartitionKey == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task PublishAsync_Should_Throw_For_Null_Message()
    {
        // Arrange
        var sut = CreatePublisher();

        // Act
        var act = () => sut.PublishAsync(null!, CancellationToken.None);

        // Assert
        return act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Group_Messages_By_Partition_Key()
    {
        // Arrange
        SetupBatches(maxEventsPerBatch: 10);
        var sut = CreatePublisher(p => p.WithPartitionKey(m => m.TenantId));
        TestMessage[] messages =
        [
            new("1", "tenant-a"),
            new("2", "tenant-b"),
            new("3", "tenant-a"),
        ];

        // Act
        await sut.PublishBatchAsync(messages, CancellationToken.None);

        // Assert
        batchOptions.Select(o => o.PartitionKey).Should().BeEquivalentTo("tenant-a", "tenant-b");
        sentBatches.Select(b => b.Count).Should().Equal(2, 1);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Start_A_New_Batch_When_Full()
    {
        // Arrange
        SetupBatches(maxEventsPerBatch: 2);
        var sut = CreatePublisher();
        var messages = Enumerable.Range(1, 5)
            .Select(i => new TestMessage(i.ToString(CultureInfo.InvariantCulture), "tenant"))
            .ToArray();

        // Act
        await sut.PublishBatchAsync(messages, CancellationToken.None);

        // Assert
        sentBatches.Select(b => b.Count).Should().Equal(2, 2, 1);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Send_To_Partition_Id()
    {
        // Arrange
        SetupBatches(maxEventsPerBatch: 10);
        var sut = CreatePublisher(p => p.WithPartitionKey(m => m.TenantId));

        // Act
        await sut.PublishBatchAsync(
            [new TestMessage("1", "tenant-a"), new TestMessage("2", "tenant-b")],
            new EventHubPublishingOptions { PartitionId = "1" },
            CancellationToken.None);

        // Assert
        batchOptions.Should().ContainSingle()
            .Which.Should().Match<CreateBatchOptions>(o => o.PartitionId == "1" && o.PartitionKey == null);
        sentBatches.Should().ContainSingle().Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Throw_When_A_Message_Does_Not_Fit_In_An_Empty_Batch()
    {
        // Arrange
        SetupBatches(maxEventsPerBatch: 0);
        var sut = CreatePublisher();

        // Act
        var act = () => sut.PublishBatchAsync([new TestMessage("1", "tenant")], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*too large*");
        sentBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Not_Send_Empty_Batches()
    {
        // Arrange
        SetupBatches(maxEventsPerBatch: 10);
        var sut = CreatePublisher();

        // Act
        await sut.PublishBatchAsync([], CancellationToken.None);

        // Assert
        sentBatches.Should().BeEmpty();
    }

    private EventHubPublisher<TestMessage> CreatePublisher(
        Action<EventHubPublisherBuilder<TestMessage>>? configure = null)
    {
        var builder = new EventHubPublisherBuilder<TestMessage>();
        configure?.Invoke(builder);

        return new EventHubPublisher<TestMessage>(producer, new JsonMessageSerializer(), builder);
    }

    private EventData GetSingleSentEvent()
        => producer.ReceivedCalls().Should().ContainSingle()
            .Which.GetArguments()[0].Should().BeAssignableTo<IEnumerable<EventData>>()
            .Which.Should().ContainSingle().Subject;

    private void SetupBatches(int maxEventsPerBatch)
    {
        producer
            .CreateBatchAsync(Arg.Any<CreateBatchOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var options = call.Arg<CreateBatchOptions>();
                batchOptions.Add(options);

                var store = new List<EventData>();
                var batch = EventHubsModelFactory.EventDataBatch(
                    batchSizeBytes: 1024,
                    batchEventStore: store,
                    batchOptions: options,
                    tryAddCallback: _ => store.Count < maxEventsPerBatch);
                batchStores[batch] = store;

                return new ValueTask<EventDataBatch>(batch);
            });

        producer
            .SendAsync(Arg.Any<EventDataBatch>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sentBatches.Add([.. batchStores[call.Arg<EventDataBatch>()]]);
                return Task.CompletedTask;
            });
    }
}