namespace Atc.Azure.Messaging.StorageQueue.Tests;

public sealed class StorageQueueMetadataTests
{
    [Fact]
    public void Create_Should_Map_Queue_Message()
    {
        // Arrange
        var message = StorageQueueTestData.CreateMessage(new TestMessage("1", "tenant"), dequeueCount: 2);

        // Act
        var metadata = StorageQueueMetadata.Create(message, "orders");

        // Assert
        metadata.MessageId.Should().Be("1");
        metadata.QueueName.Should().Be("orders");
        metadata.DequeueCount.Should().Be(2);
        metadata.PopReceipt.Should().Be("receipt-1");
        metadata.EnqueuedTime.Should().Be(StorageQueueTestData.InsertedOn);
        metadata.NextVisibleOn.Should().Be(StorageQueueTestData.InsertedOn.AddSeconds(30));
        metadata.ExpiresOn.Should().Be(StorageQueueTestData.InsertedOn.AddDays(7));
    }
}