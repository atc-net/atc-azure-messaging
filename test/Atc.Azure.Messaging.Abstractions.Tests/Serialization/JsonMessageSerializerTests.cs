namespace Atc.Azure.Messaging.Abstractions.Tests.Serialization;

public sealed class JsonMessageSerializerTests
{
    [Fact]
    public void Serialize_Should_Use_CamelCase_By_Default()
    {
        // Arrange
        var sut = new JsonMessageSerializer();

        // Act
        var payload = sut.Serialize(new TestMessage("order-1", 42));

        // Assert
        payload.ToString().Should().Be("""{"id":"order-1","quantity":42}""");
    }

    [Fact]
    public void Deserialize_Should_Be_Case_Insensitive_By_Default()
    {
        // Arrange
        var sut = new JsonMessageSerializer();

        // Act
        var message = sut.Deserialize<TestMessage>(BinaryData.FromString("""{"ID":"order-1","Quantity":42}"""));

        // Assert
        message.Should().Be(new TestMessage("order-1", 42));
    }

    [Fact]
    public void Serialize_Should_Use_The_Provided_Options()
    {
        // Arrange
        var sut = new JsonMessageSerializer(new JsonSerializerOptions());

        // Act
        var payload = sut.Serialize(new TestMessage("order-1", 42));

        // Assert
        payload.ToString().Should().Be("""{"Id":"order-1","Quantity":42}""");
    }

    [Fact]
    public void Serialize_And_Deserialize_Should_Round_Trip()
    {
        // Arrange
        var sut = new JsonMessageSerializer();
        var message = new TestMessage("order-1", 42);

        // Act
        var result = sut.Deserialize<TestMessage>(sut.Serialize(message));

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void ContentType_Should_Be_Json()
        => new JsonMessageSerializer().ContentType.Should().Be("application/json");

    [Fact]
    public void Deserialize_Should_Throw_For_Invalid_Json()
    {
        // Arrange
        var sut = new JsonMessageSerializer();

        // Act
        var act = () => sut.Deserialize<TestMessage>(BinaryData.FromString("not json"));

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Are_Null()
    {
        // Act
        var act = () => new JsonMessageSerializer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed record TestMessage(
        string Id,
        int Quantity);
}