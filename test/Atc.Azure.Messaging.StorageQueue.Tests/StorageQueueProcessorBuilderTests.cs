namespace Atc.Azure.Messaging.StorageQueue.Tests;

public sealed class StorageQueueProcessorBuilderTests
{
    [Fact]
    public void Defaults_Should_Use_A_Poison_Queue_Named_After_The_Queue()
    {
        // Act
        var sut = new StorageQueueProcessorBuilder("orders");

        // Assert
        sut.PoisonQueueName.Should().Be("orders-poison");
        sut.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
        sut.MaxMessages.Should().Be(16);
        sut.VisibilityTimeout.Should().Be(TimeSpan.FromSeconds(30));
        sut.MaxDequeueCount.Should().Be(5);
        sut.CreateQueuesIfNotExist.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void WithMaxMessages_Should_Reject_Values_Outside_The_Service_Limits(
        int maxMessages)
    {
        // Arrange
        var sut = new StorageQueueProcessorBuilder("orders");

        // Act
        var act = () => sut.WithMaxMessages(maxMessages);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithMaxDequeueCount_Should_Reject_Values_Below_One()
    {
        // Arrange
        var sut = new StorageQueueProcessorBuilder("orders");

        // Act
        var act = () => sut.WithMaxDequeueCount(0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}