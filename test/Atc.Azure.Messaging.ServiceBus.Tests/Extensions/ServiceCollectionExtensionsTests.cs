namespace Atc.Azure.Messaging.ServiceBus.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=a2V5";
    private const string OtherConnectionString = "Endpoint=sb://other.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=a2V5";

    [Fact]
    public void AddPublisher_Should_Register_Typed_And_Abstract_Publisher()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddServiceBusMessaging(sb => sb
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("queue"))
            .BuildServiceProvider();

        // Act
        var publisher = services.GetRequiredService<IServiceBusPublisher<TestMessage>>();

        // Assert
        services.GetRequiredService<IMessagePublisher<TestMessage>>().Should().BeSameAs(publisher);
        services.GetRequiredService<IServiceBusPublisherFactory>().Create<TestMessage>().Should().BeSameAs(publisher);
    }

    [Fact]
    public void AddPublisher_Should_Register_Keyed_Publishers_For_Named_Connections()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddServiceBusMessaging("orders", sb => sb
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("queue"))
            .BuildServiceProvider();

        // Act
        var keyed = services.GetRequiredKeyedService<IServiceBusPublisher<TestMessage>>("orders");

        // Assert
        services.GetRequiredKeyedService<IMessagePublisher<TestMessage>>("orders").Should().BeSameAs(keyed);
        services.GetRequiredService<IServiceBusPublisher<TestMessage>>().Should().BeSameAs(keyed);
    }

    [Fact]
    public void AddPublisher_Should_Prefer_The_Default_Connection_For_Unkeyed_Publishers()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddServiceBusMessaging("other", sb => sb
                .Configure(o => o.WithConnectionString(OtherConnectionString))
                .AddPublisher<TestMessage>("queue"))
            .AddServiceBusMessaging(sb => sb
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("queue"))
            .BuildServiceProvider();

        // Act
        var unkeyed = services.GetRequiredService<IServiceBusPublisher<TestMessage>>();

        // Assert
        unkeyed.Should().BeSameAs(services.GetRequiredService<IServiceBusPublisherFactory>().Create<TestMessage>());
        unkeyed.Should().NotBeSameAs(services.GetRequiredKeyedService<IServiceBusPublisher<TestMessage>>("other"));
    }

    [Fact]
    public void AddPublisher_Should_Throw_On_Resolve_When_The_Connection_Is_Not_Configured()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddServiceBusMessaging(sb => sb.AddPublisher<TestMessage>("queue"))
            .BuildServiceProvider();

        // Act
        var act = () => services.GetRequiredService<IServiceBusPublisher<TestMessage>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*(default) is not configured*");
    }

    [Fact]
    public void Configure_Should_Bind_Options_From_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ServiceBus:ConnectionString"] = ConnectionString,
            })
            .Build();
        var services = new ServiceCollection()
            .AddServiceBusMessaging(sb => sb.Configure(configuration.GetSection("ServiceBus")))
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptionsMonitor<ServiceBusMessagingOptions>>().Get(name: null);

        // Assert
        options.ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void AddProcessor_Should_Register_Hosted_Processor_Services()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddServiceBusMessaging(sb => sb
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddProcessor<TestMessage, TestProcessor>("queue")
                .AddProcessor<TestMessage, TestProcessor>("topic", "subscription")
                .AddSessionProcessor<TestMessage, TestProcessor>("session-queue")
                .AddSessionProcessor<TestMessage, TestProcessor>("session-topic", "subscription"))
            .BuildServiceProvider();

        // Act
        var hostedServices = services.GetServices<IHostedService>().ToList();
        var processorServices = services.GetServices<IMessageProcessorService<TestProcessor>>().ToList();

        // Assert
        hostedServices.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        processorServices.Should().BeEquivalentTo(hostedServices.Cast<IMessageProcessorService<TestProcessor>>());
        processorServices.Should().AllSatisfy(p => p.IsRunning.Should().BeFalse());
        processorServices[0].Processor.Should().BeSameAs(services.GetRequiredService<TestProcessor>());
    }

    [Fact]
    public void AddProcessor_Should_Keep_An_Existing_Processor_Registration()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddScoped<TestProcessor>()
            .AddServiceBusMessaging(sb => sb
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddProcessor<TestMessage, TestProcessor>("queue"));

        // Act
        var registrations = services.Where(d => d.ServiceType == typeof(TestProcessor)).ToList();

        // Assert
        registrations.Should().ContainSingle()
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}