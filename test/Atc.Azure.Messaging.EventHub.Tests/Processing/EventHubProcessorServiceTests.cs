namespace Atc.Azure.Messaging.EventHub.Tests.Processing;

public sealed class EventHubProcessorServiceTests
{
    private readonly IEventHubProcessor processor = Substitute.For<IEventHubProcessor>();

    [Fact]
    public async Task StartAsync_And_StopAsync_Should_Control_The_Processor()
    {
        // Arrange
        var sut = new EventHubProcessorService<TestProcessor>(processor, () => new TestProcessor());

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _ = processor.StartProcessingAsync(CancellationToken.None);
            _ = processor.StopProcessingAsync(CancellationToken.None);
        });
    }

    [Fact]
    public void IsRunning_Should_Reflect_The_Processor()
    {
        // Arrange
        processor.IsRunning.Returns(true);
        var sut = new EventHubProcessorService<TestProcessor>(processor, () => new TestProcessor());

        // Act
        var isRunning = sut.IsRunning;

        // Assert
        isRunning.Should().BeTrue();
    }

    [Fact]
    public void Processor_Should_Be_Created_Once_On_First_Use()
    {
        // Arrange
        var created = 0;
        var sut = new EventHubProcessorService<TestProcessor>(processor, () =>
        {
            created++;
            return new TestProcessor();
        });

        // Act
        var first = sut.Processor;
        var second = sut.Processor;

        // Assert
        first.Should().BeSameAs(second);
        created.Should().Be(1);
    }
}