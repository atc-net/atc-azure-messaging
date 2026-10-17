namespace Atc.Azure.Messaging.Tests.Common;

public sealed class MessageDispatcherTests
{
    private static readonly MessageMetadata Metadata = new() { MessageId = "message-1" };

    [Fact]
    public async Task DispatchAsync_Should_Hand_Message_To_Processor()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateDispatcher(processor);

        // Act
        var result = await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.Processed);
        processor.Received.Should().ContainSingle()
            .Which.Should().Be((new TestMessage("1", "tenant"), Metadata));
    }

    [Fact]
    public async Task DispatchAsync_Should_Skip_Messages_Rejected_By_A_Filter()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateDispatcher(processor, m => m.MessageId == "other");

        // Act
        var result = await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.Filtered);
        processor.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_Should_Require_All_Filters_To_Match()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateDispatcher(processor, _ => true, _ => false);

        // Act
        var result = await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.Filtered);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task DispatchAsync_Should_Report_Unreadable_Payloads(
        string payload)
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var sut = CreateDispatcher(processor);

        // Act
        var result = await sut.DispatchAsync(BinaryData.FromString(payload), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.DeserializationFailed);
        processor.Errors.Should().ContainSingle()
            .Which.Exception.Should().BeOfType<MessageDeserializationException>()
            .Which.MessageId.Should().Be("message-1");
    }

    [Fact]
    public async Task DispatchAsync_Should_Report_Processing_Failures_To_The_Error_Handler()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var sut = CreateDispatcher(processor);

        // Act
        var result = await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.ProcessingFailed);
        processor.Errors.Should().ContainSingle()
            .Which.Should().Match<(Exception Exception, MessageMetadata? Metadata)>(e =>
                e.Exception is InvalidOperationException && e.Metadata == Metadata);
    }

    [Fact]
    public async Task DispatchAsync_Should_Not_Throw_When_Processor_Fails_Without_Error_Handler()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateDispatcher(processor);

        // Act
        var result = await sut.DispatchAsync(
            Serialize(new TestMessage(TestProcessor.FailingMessageId, "tenant")),
            Metadata,
            CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.ProcessingFailed);
    }

    [Fact]
    public async Task DispatchAsync_Should_Rethrow_Cancellation_When_Stopping()
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
        var sut = CreateDispatcher(processor);

        // Act
        var act = () => sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DispatchAsync_Should_Treat_Cancellation_Without_Stopping_As_A_Failure()
    {
        // Arrange
        var processor = new TestProcessor
        {
            OnProcess = (_, _) => throw new TaskCanceledException("A timeout in user code."),
        };
        var sut = CreateDispatcher(processor);

        // Act
        var result = await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);

        // Assert
        result.Should().Be(MessageDispatchResult.ProcessingFailed);
    }

    [Fact]
    public async Task DispatchAsync_Should_Resolve_Scoped_Processors_Per_Message()
    {
        // Arrange
        ScopedTestProcessor.Reset();
        var services = new ServiceCollection()
            .AddScoped<ScopedTestProcessor>()
            .BuildServiceProvider(validateScopes: true);
        var sut = new MessageDispatcher<TestMessage, ScopedTestProcessor, MessageMetadata>(
            services.GetRequiredService<IServiceScopeFactory>(),
            new JsonMessageSerializer(),
            [],
            NullLogger.Instance);

        // Act
        await sut.DispatchAsync(Serialize(new TestMessage("1", "tenant")), Metadata, CancellationToken.None);
        await sut.DispatchAsync(Serialize(new TestMessage("2", "tenant")), Metadata, CancellationToken.None);

        // Assert
        ScopedTestProcessor.HandledBy.Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ReportErrorAsync_Should_Pass_Transport_Errors_To_The_Error_Handler()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var sut = CreateDispatcher(processor);
        var exception = new TimeoutException();

        // Act
        await sut.ReportErrorAsync(exception, metadata: null, CancellationToken.None);

        // Assert
        processor.Errors.Should().ContainSingle()
            .Which.Should().Be((exception, null));
    }

    private static BinaryData Serialize(TestMessage message)
        => TestDispatcher.Serialize(message);

    private static MessageDispatcher<TestMessage, TProcessor, MessageMetadata> CreateDispatcher<TProcessor>(
        TProcessor processor,
        params Func<MessageMetadata, bool>[] filters)
        where TProcessor : class, IMessageProcessor<TestMessage>
        => TestDispatcher.Create(processor, filters);
}