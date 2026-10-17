namespace Atc.Azure.Messaging.EventHub.Tests.Publishing;

public sealed class EventHubPublisherFactoryTests
{
    [Fact]
    public void Create_Should_Return_The_Same_Publisher_For_A_Message_Type()
    {
        // Arrange
        var sut = CreateFactory(new EventHubPublisherRegistration(typeof(TestMessage), null, "hub", new EventHubPublisherBuilder<TestMessage>()));

        // Act
        var first = sut.Create<TestMessage>();
        var second = sut.Create<TestMessage>();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Create_Should_Throw_When_No_Publisher_Is_Registered()
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var act = () => sut.Create<TestMessage>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No Event Hub publisher is registered for TestMessage*(default)*");
    }

    [Fact]
    public void Create_Should_Only_Match_Publishers_On_The_Requested_Connection()
    {
        // Arrange
        var sut = CreateFactory(new EventHubPublisherRegistration(typeof(TestMessage), "other", "hub", new EventHubPublisherBuilder<TestMessage>()));

        // Act
        var act = () => sut.Create<TestMessage>();

        // Assert
        act.Should().Throw<InvalidOperationException>();
        sut.Create<TestMessage>("other").Should().NotBeNull();
    }

    [Fact]
    public void Create_Should_Use_The_Producer_For_The_Registered_Event_Hub()
    {
        // Arrange
        var clientFactory = CreateClientFactory();
        var sut = new EventHubPublisherFactory(
            clientFactory,
            [new EventHubPublisherRegistration(typeof(TestMessage), "orders", "order-events", new EventHubPublisherBuilder<TestMessage>())]);

        // Act
        sut.Create<TestMessage>("orders");

        // Assert
        clientFactory.Received(1).GetProducerClient("orders", "order-events");
    }

    private static EventHubPublisherFactory CreateFactory(
        params EventHubPublisherRegistration[] registrations)
        => new(CreateClientFactory(), registrations);

    private static IEventHubClientFactory CreateClientFactory()
    {
        var clientFactory = Substitute.For<IEventHubClientFactory>();
        clientFactory
            .GetOptions(Arg.Any<string?>())
            .Returns(new EventHubMessagingOptions());
        clientFactory
            .GetProducerClient(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(Substitute.For<EventHubProducerClient>());

        return clientFactory;
    }
}