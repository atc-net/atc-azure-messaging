namespace Atc.Azure.Messaging.ServiceBus.Tests.Publishing;

public sealed class ServiceBusPublisherFactoryTests
{
    private readonly IServiceBusClientFactory clientFactory = Substitute.For<IServiceBusClientFactory>();

    public ServiceBusPublisherFactoryTests()
    {
        clientFactory
            .GetOptions(Arg.Any<string?>())
            .Returns(new ServiceBusMessagingOptions());
        clientFactory
            .GetSender(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<ServiceBusSenderOptions?>())
            .Returns(Substitute.For<ServiceBusSender>());
    }

    [Fact]
    public void Create_Should_Return_The_Same_Publisher_For_A_Message_Type()
    {
        // Arrange
        var sut = CreateFactory(new ServiceBusPublisherRegistration(typeof(TestMessage), null, "queue", new ServiceBusPublisherBuilder<TestMessage>()));

        // Act
        var first = sut.Create<TestMessage>();
        var second = sut.Create<TestMessage>();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Create_Should_Throw_When_No_Publisher_Is_Registered_On_The_Connection()
    {
        // Arrange
        var sut = CreateFactory(new ServiceBusPublisherRegistration(typeof(TestMessage), "other", "queue", new ServiceBusPublisherBuilder<TestMessage>()));

        // Act
        var act = () => sut.Create<TestMessage>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No Service Bus publisher is registered for TestMessage*(default)*");
    }

    [Fact]
    public void Create_Should_Use_The_Sender_For_The_Registered_Entity()
    {
        // Arrange
        var senderOptions = new ServiceBusSenderOptions { Identifier = "test" };
        var builder = new ServiceBusPublisherBuilder<TestMessage>().WithSenderOptions(senderOptions);
        var sut = CreateFactory(new ServiceBusPublisherRegistration(typeof(TestMessage), "orders", "order-queue", builder));

        // Act
        sut.Create<TestMessage>("orders");

        // Assert
        clientFactory.Received(1).GetSender("orders", "order-queue", senderOptions);
    }

    private ServiceBusPublisherFactory CreateFactory(
        params ServiceBusPublisherRegistration[] registrations)
        => new(clientFactory, registrations);
}