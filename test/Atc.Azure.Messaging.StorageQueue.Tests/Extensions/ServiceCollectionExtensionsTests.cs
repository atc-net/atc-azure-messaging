namespace Atc.Azure.Messaging.StorageQueue.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public void AddPublisher_Should_Register_Typed_And_Abstract_Publisher()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddStorageQueueMessaging(sq => sq
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("orders"))
            .BuildServiceProvider();

        // Act
        var publisher = services.GetRequiredService<IStorageQueuePublisher<TestMessage>>();

        // Assert
        services.GetRequiredService<IMessagePublisher<TestMessage>>().Should().BeSameAs(publisher);
        services.GetRequiredService<IStorageQueuePublisherFactory>().Create<TestMessage>().Should().BeSameAs(publisher);
    }

    [Fact]
    public void AddPublisher_Should_Register_Keyed_Publishers_For_Named_Connections()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddStorageQueueMessaging("orders", sq => sq
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("orders"))
            .BuildServiceProvider();

        // Act
        var keyed = services.GetRequiredKeyedService<IStorageQueuePublisher<TestMessage>>("orders");

        // Assert
        services.GetRequiredKeyedService<IMessagePublisher<TestMessage>>("orders").Should().BeSameAs(keyed);
        services.GetRequiredService<IStorageQueuePublisher<TestMessage>>().Should().BeSameAs(keyed);
    }

    [Fact]
    public void AddPublisher_Should_Throw_On_Resolve_When_The_Connection_Is_Not_Configured()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddStorageQueueMessaging(sq => sq.AddPublisher<TestMessage>("orders"))
            .BuildServiceProvider();

        // Act
        var act = () => services.GetRequiredService<IStorageQueuePublisher<TestMessage>>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*(default) is not configured*");
    }

    [Fact]
    public void Create_Should_Throw_When_No_Publisher_Is_Registered_On_The_Connection()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddStorageQueueMessaging("other", sq => sq
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddPublisher<TestMessage>("orders"))
            .BuildServiceProvider();

        // Act
        var act = () => services.GetRequiredService<IStorageQueuePublisherFactory>().Create<TestMessage>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestMessage*(default)*");
    }

    [Fact]
    public void Configure_Should_Bind_Options_From_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["StorageQueue:QueueServiceUri"] = "https://account.queue.core.windows.net",
            })
            .Build();
        var services = new ServiceCollection()
            .AddStorageQueueMessaging(sq => sq.Configure(configuration.GetSection("StorageQueue")))
            .BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptionsMonitor<StorageQueueMessagingOptions>>().Get(name: null);

        // Assert
        options.QueueServiceUri.Should().Be(new Uri("https://account.queue.core.windows.net"));
    }

    [Fact]
    public void AddProcessor_Should_Register_Hosted_Processor_Services()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddStorageQueueMessaging(sq => sq
                .Configure(o => o.WithConnectionString(ConnectionString))
                .AddProcessor<TestMessage, TestProcessor>("orders")
                .AddProcessor<TestMessage, TestProcessor>("invoices", p => p.WithPoisonQueue("invoices-failed")))
            .BuildServiceProvider();

        // Act
        var hostedServices = services.GetServices<IHostedService>().ToList();
        var processorServices = services.GetServices<IMessageProcessorService<TestProcessor>>().ToList();

        // Assert
        hostedServices.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        processorServices.Should().BeEquivalentTo(hostedServices.Cast<IMessageProcessorService<TestProcessor>>());
        processorServices[0].Processor.Should().BeSameAs(services.GetRequiredService<TestProcessor>());
    }
}