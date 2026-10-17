namespace Atc.Azure.Messaging.EventHub.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private const string ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=a2V5";
    private const string OtherConnectionString = "Endpoint=sb://other.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=a2V5";
    private const string StorageConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public void AddPublisher_Should_Register_Typed_And_Abstract_Publisher()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("hub"))
            .BuildServiceProvider();

        // Act
        var publisher = services.GetRequiredService<IEventHubPublisher<TestMessage>>();
        var messagePublisher = services.GetRequiredService<IMessagePublisher<TestMessage>>();

        // Assert
        publisher.Should().BeSameAs(messagePublisher);
        services.GetRequiredService<IEventHubPublisherFactory>().Create<TestMessage>().Should().BeSameAs(publisher);
    }

    [Fact]
    public void AddPublisher_Should_Register_Keyed_Publishers_For_Named_Connections()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging("orders", eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("hub"))
            .BuildServiceProvider();

        // Act
        var keyed = services.GetRequiredKeyedService<IEventHubPublisher<TestMessage>>("orders");

        // Assert
        services.GetRequiredKeyedService<IMessagePublisher<TestMessage>>("orders").Should().BeSameAs(keyed);
        services.GetRequiredService<IEventHubPublisher<TestMessage>>().Should().BeSameAs(keyed);
        services.GetRequiredService<IMessagePublisher<TestMessage>>().Should().BeSameAs(keyed);
    }

    [Fact]
    public void AddPublisher_Should_Prefer_The_Default_Connection_For_Unkeyed_Publishers()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging("other", eh => eh
                .Configure(o => o.WithConnectionString(OtherConnectionString))
                .AddPublisher<TestMessage>("hub"))
            .AddEventHubMessaging(eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("hub"))
            .BuildServiceProvider();

        // Act
        var unkeyed = services.GetRequiredService<IEventHubPublisher<TestMessage>>();

        // Assert
        unkeyed.Should().BeSameAs(services.GetRequiredService<IEventHubPublisherFactory>().Create<TestMessage>());
        unkeyed.Should().NotBeSameAs(services.GetRequiredKeyedService<IEventHubPublisher<TestMessage>>("other"));
    }

    [Fact]
    public void AddPublisher_Should_Throw_On_Resolve_When_The_Connection_Is_Not_Configured()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh.AddPublisher<TestMessage>("hub"))
            .BuildServiceProvider();

        // Act
        var act = () => services.GetRequiredService<IEventHubPublisher<TestMessage>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection (default) is not configured*");
    }

    [Fact]
    public void Configure_Should_Bind_Options_From_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["EventHub:FullyQualifiedNamespace"] = "test.servicebus.windows.net",
            })
            .Build();
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh.Configure(configuration.GetSection("EventHub")))
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptionsMonitor<EventHubMessagingOptions>>().Get(name: null);

        // Assert
        options.FullyQualifiedNamespace.Should().Be("test.servicebus.windows.net");
    }

    [Fact]
    public void AddProcessor_Should_Register_A_Hosted_Processor_Service()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh
                .Configure(o => o
                    .WithConnectionString(ConnectionString)
                    .WithCheckpointStorage(StorageConnectionString))
                .AddProcessor<TestMessage, TestProcessor>("hub"))
            .BuildServiceProvider();

        // Act
        var hostedServices = services.GetServices<IHostedService>().ToList();
        var processorService = services.GetRequiredService<IMessageProcessorService<TestProcessor>>();

        // Assert
        hostedServices.Should().ContainSingle().Which.Should().BeSameAs(processorService);
        processorService.Processor.Should().BeSameAs(services.GetRequiredService<TestProcessor>());
        processorService.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void AddProcessor_Should_Throw_On_Resolve_Without_Checkpoint_Storage()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddProcessor<TestMessage, TestProcessor>("hub"))
            .BuildServiceProvider();

        // Act
        var act = () => services.GetServices<IHostedService>().ToList();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Checkpoint storage is not configured*");
    }

    [Fact]
    public void AddStatelessProcessor_Should_Allow_The_Same_Processor_Type_More_Than_Once()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddEventHubMessaging(eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddStatelessProcessor<TestMessage, TestProcessor>("hub-a")
                .AddStatelessProcessor<TestMessage, TestProcessor>("hub-b"))
            .BuildServiceProvider();

        // Act
        var hostedServices = services.GetServices<IHostedService>().ToList();

        // Assert
        hostedServices.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        services.GetServices<IMessageProcessorService<TestProcessor>>().Should().HaveCount(2);
    }

    [Fact]
    public void AddProcessor_Should_Keep_An_Existing_Processor_Registration()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddScoped<TestProcessor>()
            .AddEventHubMessaging(eh => eh
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddStatelessProcessor<TestMessage, TestProcessor>("hub"));

        // Act
        var registrations = services.Where(d => d.ServiceType == typeof(TestProcessor)).ToList();

        // Assert
        registrations.Should().ContainSingle()
            .Which.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}