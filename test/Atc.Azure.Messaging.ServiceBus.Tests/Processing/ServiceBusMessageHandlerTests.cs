namespace Atc.Azure.Messaging.ServiceBus.Tests.Processing;

public sealed class ServiceBusMessageHandlerTests
{
    private readonly IMessageSettlement settlement = Substitute.For<IMessageSettlement>();

    [Fact]
    public async Task HandleAsync_Should_Complete_Processed_Messages()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateHandler(processor);
        var message = ServiceBusTestData.CreateMessage(new TestMessage("1", "tenant"));

        // Act
        await sut.HandleAsync(message, settlement, CancellationToken.None);

        // Assert
        processor.Received.Should().ContainSingle()
            .Which.Metadata.Should().BeOfType<ServiceBusMetadata>()
            .Which.SessionId.Should().Be("session-1");
        await settlement.Received(1).CompleteAsync(message, CancellationToken.None);
        await settlement.DidNotReceiveWithAnyArgs().AbandonAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_Should_Complete_Filtered_Messages()
    {
        // Arrange
        var processor = new TestProcessor();
        var sut = CreateHandler(processor, m => m.Subject == "other");
        var message = ServiceBusTestData.CreateMessage(new TestMessage("1", "tenant"));

        // Act
        await sut.HandleAsync(message, settlement, CancellationToken.None);

        // Assert
        processor.Received.Should().BeEmpty();
        await settlement.Received(1).CompleteAsync(message, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_Should_Dead_Letter_Unreadable_Messages()
    {
        // Arrange
        var sut = CreateHandler(new TestProcessor());
        var message = ServiceBusTestData.CreateMessage(BinaryData.FromString("not json"), "1");

        // Act
        await sut.HandleAsync(message, settlement, CancellationToken.None);

        // Assert
        await settlement.Received(1).DeadLetterAsync(
            message,
            ServiceBusMessageHandler<TestMessage, TestProcessor>.DeserializationFailedReason,
            Arg.Is<string>(s => s.Contains(nameof(TestMessage), StringComparison.Ordinal)),
            CancellationToken.None);
        await settlement.DidNotReceiveWithAnyArgs().CompleteAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_Should_Abandon_Failed_Messages()
    {
        // Arrange
        var sut = CreateHandler(new TestProcessor());
        var message = ServiceBusTestData.CreateMessage(new TestMessage(TestProcessor.FailingMessageId, "tenant"));

        // Act
        await sut.HandleAsync(message, settlement, CancellationToken.None);

        // Assert
        await settlement.Received(1).AbandonAsync(message, CancellationToken.None);
        await settlement.DidNotReceiveWithAnyArgs().CompleteAsync(default!, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Settle_When_Stopping()
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
        var sut = CreateHandler(processor);
        var message = ServiceBusTestData.CreateMessage(new TestMessage("1", "tenant"));

        // Act
        var act = () => sut.HandleAsync(message, settlement, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        settlement.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task HandleErrorAsync_Should_Report_Errors_To_The_Processor()
    {
        // Arrange
        var processor = new TestErrorHandlingProcessor();
        var sut = CreateHandler(processor);
        var exception = new ServiceBusException("Connection lost.", ServiceBusFailureReason.ServiceCommunicationProblem);

        // Act
        await sut.HandleErrorAsync(new ProcessErrorEventArgs(
            exception,
            ServiceBusErrorSource.Receive,
            "test.servicebus.windows.net",
            "queue",
            "identifier",
            CancellationToken.None));

        // Assert
        processor.Errors.Should().ContainSingle()
            .Which.Should().Be((exception, null));
    }

    private static ServiceBusMessageHandler<TestMessage, TProcessor> CreateHandler<TProcessor>(
        TProcessor processor,
        params Func<ServiceBusMetadata, bool>[] filters)
        where TProcessor : class, IMessageProcessor<TestMessage>
        => new(TestDispatcher.Create(processor, filters));
}