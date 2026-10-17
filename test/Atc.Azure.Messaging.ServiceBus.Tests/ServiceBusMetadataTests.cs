namespace Atc.Azure.Messaging.ServiceBus.Tests;

public sealed class ServiceBusMetadataTests
{
    [Fact]
    public void Create_Should_Map_Received_Message()
    {
        // Arrange
        var message = ServiceBusTestData.CreateMessage(new TestMessage("1", "tenant"), deliveryCount: 3);

        // Act
        var metadata = ServiceBusMetadata.Create(message);

        // Assert
        metadata.MessageId.Should().Be("1");
        metadata.CorrelationId.Should().Be("correlation-1");
        metadata.ContentType.Should().Be("application/json");
        metadata.PartitionKey.Should().Be("session-1");
        metadata.EnqueuedTime.Should().Be(ServiceBusTestData.EnqueuedTime);
        metadata.Properties.Should().Contain("TenantId", "tenant");
        metadata.SessionId.Should().Be("session-1");
        metadata.ReplyToSessionId.Should().Be("reply-session");
        metadata.Subject.Should().Be("test-subject");
        metadata.ReplyTo.Should().Be("reply-to");
        metadata.To.Should().Be("to");
        metadata.DeliveryCount.Should().Be(3);
        metadata.SequenceNumber.Should().Be(42);
        metadata.EnqueuedSequenceNumber.Should().Be(41);
        metadata.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
        metadata.LockedUntil.Should().Be(ServiceBusTestData.EnqueuedTime.AddMinutes(1));
        metadata.State.Should().Be(ServiceBusMessageState.Active);
    }

    [Fact]
    public void Create_Should_Throw_For_Null_Message()
    {
        // Act
        var act = () => ServiceBusMetadata.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}