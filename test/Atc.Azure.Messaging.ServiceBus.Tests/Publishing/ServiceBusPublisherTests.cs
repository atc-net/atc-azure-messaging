namespace Atc.Azure.Messaging.ServiceBus.Tests.Publishing;

public sealed class ServiceBusPublisherTests
{
    private readonly ServiceBusSender sender = Substitute.For<ServiceBusSender>();
    private readonly List<List<ServiceBusMessage>> sentBatches = [];
    private readonly Dictionary<ServiceBusMessageBatch, List<ServiceBusMessage>> batchStores = new(ReferenceEqualityComparer.Instance);

    [Fact]
    public async Task PublishAsync_Should_Send_Serialized_Message()
    {
        // Arrange
        var sut = CreatePublisher();
        var message = new TestMessage("1", "tenant");

        // Act
        await sut.PublishAsync(message, CancellationToken.None);

        // Assert
        var sent = GetSingleSentMessage();
        sent.Body.ToObjectFromJson<TestMessage>(JsonSerializerOptions.Web).Should().Be(message);
        sent.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task PublishAsync_Should_Apply_Publisher_Defaults()
    {
        // Arrange
        var sut = CreatePublisher(p => p
            .WithMessageId(m => m.Id)
            .WithCorrelationId(m => $"correlation-{m.Id}")
            .WithSessionId(m => m.TenantId)
            .WithSubject("test-subject")
            .WithTimeToLive(TimeSpan.FromMinutes(5))
            .WithProperty(m => m.TenantId)
            .WithProperty("origin", "tests"));

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), CancellationToken.None);

        // Assert
        var sent = GetSingleSentMessage();
        sent.MessageId.Should().Be("1");
        sent.CorrelationId.Should().Be("correlation-1");
        sent.SessionId.Should().Be("tenant");
        sent.Subject.Should().Be("test-subject");
        sent.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
        sent.ApplicationProperties.Should().Contain("TenantId", "tenant").And.Contain("origin", "tests");
    }

    [Fact]
    public async Task PublishAsync_Should_Let_Options_Override_Defaults()
    {
        // Arrange
        var sut = CreatePublisher(p => p
            .WithMessageId(m => m.Id)
            .WithSubject("test-subject"));
        var options = new ServiceBusPublishingOptions
        {
            MessageId = "custom-id",
            CorrelationId = "custom-correlation",
            ContentType = "application/custom",
            SessionId = "custom-session",
            PartitionKey = "custom-session",
            Subject = "custom-subject",
            TimeToLive = TimeSpan.FromMinutes(1),
            ReplyTo = "reply-to",
            ReplyToSessionId = "reply-session",
            To = "to",
            Properties = { ["origin"] = "options" },
        };

        // Act
        await sut.PublishAsync(new TestMessage("1", "tenant"), options, CancellationToken.None);

        // Assert
        var sent = GetSingleSentMessage();
        sent.MessageId.Should().Be("custom-id");
        sent.CorrelationId.Should().Be("custom-correlation");
        sent.ContentType.Should().Be("application/custom");
        sent.SessionId.Should().Be("custom-session");
        sent.PartitionKey.Should().Be("custom-session");
        sent.Subject.Should().Be("custom-subject");
        sent.TimeToLive.Should().Be(TimeSpan.FromMinutes(1));
        sent.ReplyTo.Should().Be("reply-to");
        sent.ReplyToSessionId.Should().Be("reply-session");
        sent.To.Should().Be("to");
        sent.ApplicationProperties.Should().Contain("origin", "options");
    }

    [Fact]
    public async Task PublishAsync_Should_Apply_Generic_Options()
    {
        // Arrange
        var sut = CreatePublisher();

        // Act
        await sut.PublishAsync(
            new TestMessage("1", "tenant"),
            new PublishingOptions { MessageId = "custom-id", PartitionKey = "key" },
            CancellationToken.None);

        // Assert
        var sent = GetSingleSentMessage();
        sent.MessageId.Should().Be("custom-id");
        sent.PartitionKey.Should().Be("key");
    }

    [Fact]
    public async Task SchedulePublishAsync_Should_Schedule_The_Message()
    {
        // Arrange
        var scheduledTime = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);
        sender
            .ScheduleMessageAsync(Arg.Any<ServiceBusMessage>(), scheduledTime, Arg.Any<CancellationToken>())
            .Returns(17L);
        var sut = CreatePublisher(p => p.WithMessageId(m => m.Id));

        // Act
        var sequenceNumber = await sut.SchedulePublishAsync(
            new TestMessage("1", "tenant"),
            scheduledTime,
            cancellationToken: CancellationToken.None);

        // Assert
        sequenceNumber.Should().Be(17);
        await sender.Received(1).ScheduleMessageAsync(
            Arg.Is<ServiceBusMessage>(m => m.MessageId == "1"),
            scheduledTime,
            CancellationToken.None);
    }

    [Fact]
    public async Task CancelScheduledPublishAsync_Should_Cancel_The_Scheduled_Message()
    {
        // Arrange
        var sut = CreatePublisher();

        // Act
        await sut.CancelScheduledPublishAsync(17, CancellationToken.None);

        // Assert
        await sender.Received(1).CancelScheduledMessageAsync(17, CancellationToken.None);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Group_Messages_By_Session()
    {
        // Arrange
        SetupBatches(maxMessagesPerBatch: 10);
        var sut = CreatePublisher(p => p.WithSessionId(m => m.TenantId));
        TestMessage[] messages =
        [
            new("1", "tenant-a"),
            new("2", "tenant-b"),
            new("3", "tenant-a"),
        ];

        // Act
        await sut.PublishBatchAsync(messages, CancellationToken.None);

        // Assert
        sentBatches.Select(b => b.Select(m => m.SessionId).Distinct(StringComparer.Ordinal).Single())
            .Should().Equal("tenant-a", "tenant-b");
        sentBatches.Select(b => b.Count).Should().Equal(2, 1);
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Start_A_New_Batch_When_Full()
    {
        // Arrange
        SetupBatches(maxMessagesPerBatch: 2);
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
    public async Task PublishBatchAsync_Should_Throw_When_A_Message_Does_Not_Fit_In_An_Empty_Batch()
    {
        // Arrange
        SetupBatches(maxMessagesPerBatch: 0);
        var sut = CreatePublisher();

        // Act
        var act = () => sut.PublishBatchAsync([new TestMessage("1", "tenant")], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*too large*");
        sentBatches.Should().BeEmpty();
    }

    private ServiceBusPublisher<TestMessage> CreatePublisher(
        Action<ServiceBusPublisherBuilder<TestMessage>>? configure = null)
    {
        var builder = new ServiceBusPublisherBuilder<TestMessage>();
        configure?.Invoke(builder);

        return new ServiceBusPublisher<TestMessage>(sender, new JsonMessageSerializer(), builder);
    }

    private ServiceBusMessage GetSingleSentMessage()
        => sender.ReceivedCalls().Should().ContainSingle()
            .Which.GetArguments()[0].Should().BeOfType<ServiceBusMessage>().Subject;

    private void SetupBatches(int maxMessagesPerBatch)
    {
        sender
            .CreateMessageBatchAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var store = new List<ServiceBusMessage>();
                var batch = ServiceBusModelFactory.ServiceBusMessageBatch(
                    batchSizeBytes: 1024,
                    batchMessageStore: store,
                    tryAddCallback: _ => store.Count < maxMessagesPerBatch);
                batchStores[batch] = store;

                return new ValueTask<ServiceBusMessageBatch>(batch);
            });

        sender
            .SendMessagesAsync(Arg.Any<ServiceBusMessageBatch>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sentBatches.Add([.. batchStores[call.Arg<ServiceBusMessageBatch>()]]);
                return Task.CompletedTask;
            });
    }
}