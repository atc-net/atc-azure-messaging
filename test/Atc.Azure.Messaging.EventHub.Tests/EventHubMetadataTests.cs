namespace Atc.Azure.Messaging.EventHub.Tests;

public sealed class EventHubMetadataTests
{
    [Fact]
    public void Create_Should_Map_Event_Data()
    {
        // Arrange
        var eventData = EventHubTestData.CreateEvent(new TestMessage("1", "tenant"), sequenceNumber: 42);

        // Act
        var metadata = EventHubMetadata.Create(eventData, "3");

        // Assert
        metadata.MessageId.Should().Be("1");
        metadata.CorrelationId.Should().Be("correlation-1");
        metadata.ContentType.Should().Be("application/json");
        metadata.PartitionKey.Should().Be("tenant");
        metadata.PartitionId.Should().Be("3");
        metadata.SequenceNumber.Should().Be(42);
        metadata.Offset.Should().Be("4200");
        metadata.EnqueuedTime.Should().Be(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
        metadata.Properties.Should().Contain("TenantId", "tenant");
        metadata.SystemProperties.Should().Contain("x-opt-system", "value");
    }

    [Fact]
    public void Create_Should_Copy_Properties()
    {
        // Arrange
        var eventData = EventHubTestData.CreateEvent(new TestMessage("1", "tenant"));

        // Act
        var metadata = EventHubMetadata.Create(eventData, "0");
        eventData.Properties["TenantId"] = "changed";

        // Assert
        metadata.Properties.Should().Contain("TenantId", "tenant");
    }
}