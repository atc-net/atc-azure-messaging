namespace Atc.Azure.Messaging.EventHub.Builders;

/// <summary>
/// Configures an Event Hubs connection with its publishers and processors.
/// </summary>
public class EventHubMessagingBuilder
{
    /// <summary>
    /// The consumer group every Event Hub has.
    /// </summary>
    public const string DefaultConsumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

    internal EventHubMessagingBuilder(
        IServiceCollection services,
        string? connectionName)
    {
        Services = services;
        ConnectionName = connectionName;
    }

    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the name of the connection, or <see langword="null"/> for the default connection.
    /// </summary>
    public string? ConnectionName { get; }

    public EventHubMessagingBuilder Configure(
        Action<EventHubMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(ConnectionName, configure);
        return this;
    }

    /// <summary>
    /// Binds the connection options to a configuration section, e.g. one with a
    /// <c>FullyQualifiedNamespace</c> or <c>ConnectionString</c> value.
    /// </summary>
    /// <param name="configuration">The configuration section to bind the options to.</param>
    public EventHubMessagingBuilder Configure(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Services.Configure<EventHubMessagingOptions>(ConnectionName, configuration);
        return this;
    }

    /// <summary>
    /// Configures the connection options with an <see cref="IConfigureNamedOptions{TOptions}"/> resolved from DI.
    /// </summary>
    /// <typeparam name="TConfigureOptions">The type that configures the options, resolved from dependency injection.</typeparam>
    public EventHubMessagingBuilder Configure<TConfigureOptions>()
        where TConfigureOptions : class, IConfigureOptions<EventHubMessagingOptions>
    {
        Services.ConfigureOptions<TConfigureOptions>();
        return this;
    }

    /// <summary>
    /// Registers an <see cref="IEventHubPublisher{TMessage}"/> and <see cref="IMessagePublisher{TMessage}"/>
    /// that publish <typeparamref name="TMessage"/> to <paramref name="eventHubName"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="configure">Optionally configures defaults applied to every published message.</param>
    /// <remarks>
    /// Publishers on a named connection are registered as keyed services under the connection name. They are also
    /// registered without a key, unless a publisher for the same message type exists on the default connection.
    /// </remarks>
    public EventHubMessagingBuilder AddPublisher<TMessage>(
        string eventHubName,
        Action<EventHubPublisherBuilder<TMessage>>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);

        var publisherBuilder = new EventHubPublisherBuilder<TMessage>();
        configure?.Invoke(publisherBuilder);

        Services.AddEventHubCoreServices();
        Services.TryAddSingleton<IEventHubPublisherFactory, EventHubPublisherFactory>();
        Services.AddSingleton(new EventHubPublisherRegistration(typeof(TMessage), ConnectionName, eventHubName, publisherBuilder));

        var connectionName = ConnectionName;
        Services.AddKeyedSingleton<IEventHubPublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredService<IEventHubPublisherFactory>().Create<TMessage>(connectionName));
        Services.AddKeyedSingleton<IMessagePublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredKeyedService<IEventHubPublisher<TMessage>>(connectionName));

        if (connectionName is not null)
        {
            Services.TryAddSingleton(s => s.GetRequiredKeyedService<IEventHubPublisher<TMessage>>(connectionName));
            Services.TryAddSingleton<IMessagePublisher<TMessage>>(s => s.GetRequiredKeyedService<IEventHubPublisher<TMessage>>(connectionName));
        }

        return this;
    }

    /// <summary>
    /// Registers a hosted processor that reads <paramref name="eventHubName"/> with load balancing across instances
    /// and checkpoints in Blob Storage, handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="consumerGroup">The consumer group to read with. Defaults to <c>$Default</c>.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <remarks>
    /// <typeparamref name="TProcessor"/> is registered as a singleton unless it is already registered. It is resolved
    /// from a new scope for every event, so register it as scoped to get scoped dependencies per event.
    /// Requires checkpoint storage, see <see cref="EventHubMessagingOptions.WithCheckpointStorage(Uri, TokenCredential?, BlobClientOptions?)"/>.
    /// </remarks>
    public EventHubMessagingBuilder AddProcessor<TMessage, TProcessor>(
        string eventHubName,
        string consumerGroup = DefaultConsumerGroup,
        Action<EventHubProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        var processorBuilder = new EventHubProcessorBuilder(eventHubName);
        configure?.Invoke(processorBuilder);

        var connectionName = ConnectionName;
        return AddProcessorService<TMessage, TProcessor>(s =>
        {
            var clientFactory = s.GetRequiredService<IEventHubClientFactory>();
            var options = clientFactory.GetOptions(connectionName);
            var dispatcher = CreateDispatcher<TMessage, TProcessor>(s, options, processorBuilder.Filters);
            var container = clientFactory.CreateCheckpointContainerClient(connectionName, processorBuilder.ContainerName);

            return options.ConnectionString is { } connectionString
                ? new CheckpointedEventHubProcessor<TMessage, TProcessor>(
                    dispatcher,
                    container,
                    processorBuilder.CreateContainerIfNotExists,
                    processorBuilder.MaxBatchSize,
                    consumerGroup,
                    connectionString,
                    eventHubName,
                    processorBuilder.ProcessorOptions)
                : new CheckpointedEventHubProcessor<TMessage, TProcessor>(
                    dispatcher,
                    container,
                    processorBuilder.CreateContainerIfNotExists,
                    processorBuilder.MaxBatchSize,
                    consumerGroup,
                    options.FullyQualifiedNamespace!,
                    eventHubName,
                    options.Credential!,
                    processorBuilder.ProcessorOptions);
        });
    }

    /// <summary>
    /// Registers a hosted processor that reads every partition of <paramref name="eventHubName"/> without
    /// checkpointing, handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="consumerGroup">The consumer group to read with. Defaults to <c>$Default</c>.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <remarks>
    /// Every running instance receives every event, starting from the configured starting position
    /// (the latest event by default). Useful for cache invalidation, live dashboards and similar fan-out scenarios.
    /// </remarks>
    public EventHubMessagingBuilder AddStatelessProcessor<TMessage, TProcessor>(
        string eventHubName,
        string consumerGroup = DefaultConsumerGroup,
        Action<EventHubStatelessProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        var processorBuilder = new EventHubStatelessProcessorBuilder();
        configure?.Invoke(processorBuilder);

        var connectionName = ConnectionName;
        return AddProcessorService<TMessage, TProcessor>(s =>
        {
            var clientFactory = s.GetRequiredService<IEventHubClientFactory>();
            var options = clientFactory.GetOptions(connectionName);

            return new StatelessEventHubProcessor<TMessage, TProcessor>(
                clientFactory.CreateConsumerClient(connectionName, eventHubName, consumerGroup),
                CreateDispatcher<TMessage, TProcessor>(s, options, processorBuilder.Filters),
                processorBuilder.ReadOptions,
                processorBuilder.StartingPosition,
                s.GetService<TimeProvider>() ?? TimeProvider.System);
        });
    }

    private static MessageDispatcher<TMessage, TProcessor, EventHubMetadata> CreateDispatcher<TMessage, TProcessor>(
        IServiceProvider services,
        EventHubMessagingOptions options,
        List<Func<EventHubMetadata, bool>> filters)
        where TProcessor : class, IMessageProcessor<TMessage>
        => new(
            services.GetRequiredService<IServiceScopeFactory>(),
            options.Serializer,
            [.. filters],
            services.GetRequiredService<ILogger<TProcessor>>());

    private EventHubMessagingBuilder AddProcessorService<TMessage, TProcessor>(
        Func<IServiceProvider, IEventHubProcessor> processorFactory)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        Services.AddEventHubCoreServices();
        Services.AddLogging();
        Services.TryAddSingleton<TProcessor>();

        // A unique key per registration lets the same processor type be registered more than once.
        var key = new object();
        Services.AddKeyedSingleton(
            key,
            (s, _) => new EventHubProcessorService<TProcessor>(
                processorFactory(s),
                s.GetRequiredService<TProcessor>));
        Services.AddSingleton<IHostedService>(s => s.GetRequiredKeyedService<EventHubProcessorService<TProcessor>>(key));
        Services.AddSingleton<IMessageProcessorService<TProcessor>>(s => s.GetRequiredKeyedService<EventHubProcessorService<TProcessor>>(key));

        return this;
    }
}