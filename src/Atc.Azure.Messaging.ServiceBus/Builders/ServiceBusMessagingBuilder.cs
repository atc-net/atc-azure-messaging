namespace Atc.Azure.Messaging.ServiceBus.Builders;

/// <summary>
/// Configures a Service Bus connection with its publishers and processors.
/// </summary>
public class ServiceBusMessagingBuilder
{
    internal ServiceBusMessagingBuilder(
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

    /// <summary>
    /// Configures the connection options.
    /// </summary>
    /// <param name="configure">The delegate that configures the options.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingBuilder Configure(
        Action<ServiceBusMessagingOptions> configure)
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
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingBuilder Configure(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Services.Configure<ServiceBusMessagingOptions>(ConnectionName, configuration);
        return this;
    }

    /// <summary>
    /// Configures the connection options with an <see cref="IConfigureOptions{TOptions}"/> resolved from DI.
    /// </summary>
    /// <typeparam name="TConfigureOptions">The type that configures the options, resolved from dependency injection.</typeparam>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingBuilder Configure<TConfigureOptions>()
        where TConfigureOptions : class, IConfigureOptions<ServiceBusMessagingOptions>
    {
        Services.ConfigureOptions<TConfigureOptions>();
        return this;
    }

    /// <summary>
    /// Registers an <see cref="IServiceBusPublisher{TMessage}"/> and <see cref="IMessagePublisher{TMessage}"/>
    /// that publish <typeparamref name="TMessage"/> to <paramref name="topicOrQueueName"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="topicOrQueueName">The name of the topic or queue.</param>
    /// <param name="configure">Optionally configures defaults applied to every published message.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Publishers on a named connection are registered as keyed services under the connection name. They are also
    /// registered without a key, unless a publisher for the same message type exists on the default connection.
    /// </remarks>
    public ServiceBusMessagingBuilder AddPublisher<TMessage>(
        string topicOrQueueName,
        Action<ServiceBusPublisherBuilder<TMessage>>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicOrQueueName);

        var publisherBuilder = new ServiceBusPublisherBuilder<TMessage>();
        configure?.Invoke(publisherBuilder);

        Services.AddServiceBusCoreServices();
        Services.TryAddSingleton<IServiceBusPublisherFactory, ServiceBusPublisherFactory>();
        Services.AddSingleton(new ServiceBusPublisherRegistration(typeof(TMessage), ConnectionName, topicOrQueueName, publisherBuilder));

        var connectionName = ConnectionName;
        Services.AddKeyedSingleton<IServiceBusPublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredService<IServiceBusPublisherFactory>().Create<TMessage>(connectionName));
        Services.AddKeyedSingleton<IMessagePublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredKeyedService<IServiceBusPublisher<TMessage>>(connectionName));

        if (connectionName is not null)
        {
            Services.TryAddSingleton(s => s.GetRequiredKeyedService<IServiceBusPublisher<TMessage>>(connectionName));
            Services.TryAddSingleton<IMessagePublisher<TMessage>>(s => s.GetRequiredKeyedService<IServiceBusPublisher<TMessage>>(connectionName));
        }

        return this;
    }

    /// <summary>
    /// Registers a hosted processor for the queue <paramref name="queueName"/>,
    /// handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Messages are completed when processed or filtered out, dead-lettered when they cannot be deserialized, and
    /// abandoned when the processor throws, so the broker redelivers them until <c>MaxDeliveryCount</c> is reached.
    /// </para>
    /// <para>
    /// <typeparamref name="TProcessor"/> is registered as a singleton unless it is already registered. It is resolved
    /// from a new scope for every message, so register it as scoped to get scoped dependencies per message.
    /// </para>
    /// </remarks>
    public ServiceBusMessagingBuilder AddProcessor<TMessage, TProcessor>(
        string queueName,
        Action<ServiceBusProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        return AddProcessor<TMessage, TProcessor>(
            (client, options) => client.CreateProcessor(queueName, options),
            configure);
    }

    /// <summary>
    /// Registers a hosted processor for the subscription <paramref name="subscriptionName"/> on the topic
    /// <paramref name="topicName"/>, handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// See <see cref="AddProcessor{TMessage, TProcessor}(string, Action{ServiceBusProcessorBuilder}?)"/> for how messages are settled.
    /// </remarks>
    public ServiceBusMessagingBuilder AddProcessor<TMessage, TProcessor>(
        string topicName,
        string subscriptionName,
        Action<ServiceBusProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        return AddProcessor<TMessage, TProcessor>(
            (client, options) => client.CreateProcessor(topicName, subscriptionName, options),
            configure);
    }

    /// <summary>
    /// Registers a hosted session processor for the session enabled queue <paramref name="queueName"/>,
    /// handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/> in order per session.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingBuilder AddSessionProcessor<TMessage, TProcessor>(
        string queueName,
        Action<ServiceBusSessionProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        return AddSessionProcessor<TMessage, TProcessor>(
            (client, options) => client.CreateSessionProcessor(queueName, options),
            configure);
    }

    /// <summary>
    /// Registers a hosted session processor for the session enabled subscription <paramref name="subscriptionName"/>
    /// on the topic <paramref name="topicName"/>, handing <typeparamref name="TMessage"/> messages to
    /// <typeparamref name="TProcessor"/> in order per session.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusMessagingBuilder AddSessionProcessor<TMessage, TProcessor>(
        string topicName,
        string subscriptionName,
        Action<ServiceBusSessionProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        return AddSessionProcessor<TMessage, TProcessor>(
            (client, options) => client.CreateSessionProcessor(topicName, subscriptionName, options),
            configure);
    }

    private ServiceBusMessagingBuilder AddProcessor<TMessage, TProcessor>(
        Func<ServiceBusClient, ServiceBusProcessorOptions, ServiceBusProcessor> createProcessor,
        Action<ServiceBusProcessorBuilder>? configure)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        var processorBuilder = new ServiceBusProcessorBuilder();
        configure?.Invoke(processorBuilder);

        var connectionName = ConnectionName;
        AddCommonProcessorServices<TProcessor>();

        var key = new object();
        Services.AddKeyedSingleton(
            key,
            (s, _) =>
            {
                var clientFactory = s.GetRequiredService<IServiceBusClientFactory>();
                var processorOptions = processorBuilder.ProcessorOptions ?? new ServiceBusProcessorOptions();
                processorOptions.AutoCompleteMessages = false;

                return new ServiceBusProcessorService<TMessage, TProcessor>(
                    createProcessor(clientFactory.GetClient(connectionName), processorOptions),
                    CreateHandler<TMessage, TProcessor>(s, clientFactory.GetOptions(connectionName), processorBuilder.Filters),
                    s.GetRequiredService<TProcessor>);
            });
        Services.AddSingleton<IHostedService>(s => s.GetRequiredKeyedService<ServiceBusProcessorService<TMessage, TProcessor>>(key));
        Services.AddSingleton<IMessageProcessorService<TProcessor>>(s => s.GetRequiredKeyedService<ServiceBusProcessorService<TMessage, TProcessor>>(key));

        return this;
    }

    private ServiceBusMessagingBuilder AddSessionProcessor<TMessage, TProcessor>(
        Func<ServiceBusClient, ServiceBusSessionProcessorOptions, ServiceBusSessionProcessor> createProcessor,
        Action<ServiceBusSessionProcessorBuilder>? configure)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        var processorBuilder = new ServiceBusSessionProcessorBuilder();
        configure?.Invoke(processorBuilder);

        var connectionName = ConnectionName;
        AddCommonProcessorServices<TProcessor>();

        var key = new object();
        Services.AddKeyedSingleton(
            key,
            (s, _) =>
            {
                var clientFactory = s.GetRequiredService<IServiceBusClientFactory>();
                var processorOptions = processorBuilder.ProcessorOptions ?? new ServiceBusSessionProcessorOptions();
                processorOptions.AutoCompleteMessages = false;

                return new ServiceBusSessionProcessorService<TMessage, TProcessor>(
                    createProcessor(clientFactory.GetClient(connectionName), processorOptions),
                    CreateHandler<TMessage, TProcessor>(s, clientFactory.GetOptions(connectionName), processorBuilder.Filters),
                    s.GetRequiredService<TProcessor>);
            });
        Services.AddSingleton<IHostedService>(s => s.GetRequiredKeyedService<ServiceBusSessionProcessorService<TMessage, TProcessor>>(key));
        Services.AddSingleton<IMessageProcessorService<TProcessor>>(s => s.GetRequiredKeyedService<ServiceBusSessionProcessorService<TMessage, TProcessor>>(key));

        return this;
    }

    private void AddCommonProcessorServices<TProcessor>()
        where TProcessor : class
    {
        Services.AddServiceBusCoreServices();
        Services.AddLogging();
        Services.TryAddSingleton<TProcessor>();
    }

    private static ServiceBusMessageHandler<TMessage, TProcessor> CreateHandler<TMessage, TProcessor>(
        IServiceProvider services,
        ServiceBusMessagingOptions options,
        List<Func<ServiceBusMetadata, bool>> filters)
        where TProcessor : class, IMessageProcessor<TMessage>
        => new(
            new MessageDispatcher<TMessage, TProcessor, ServiceBusMetadata>(
                services.GetRequiredService<IServiceScopeFactory>(),
                options.Serializer,
                [.. filters],
                services.GetRequiredService<ILogger<TProcessor>>()));
}