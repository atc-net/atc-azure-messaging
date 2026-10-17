namespace Atc.Azure.Messaging.Abstractions.Tests;

public sealed class MessageDeserializationExceptionTests
{
    [Fact]
    public void Constructor_Should_Describe_Message_Type_And_Id()
    {
        // Arrange
        var inner = new JsonException("bad json");

        // Act
        var sut = new MessageDeserializationException(typeof(Uri), "message-1", inner);

        // Assert
        sut.Message.Should().Be("Message 'message-1' could not be deserialized into Uri.");
        sut.MessageType.Should().Be<Uri>();
        sut.MessageId.Should().Be("message-1");
        sut.InnerException.Should().BeSameAs(inner);
    }
}