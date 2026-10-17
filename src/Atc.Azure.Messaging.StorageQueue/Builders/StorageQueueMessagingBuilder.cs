namespace Atc.Azure.Messaging.StorageQueue.Builders;

/// <summary>
/// Configures a Storage Queue connection with its publishers and processors.
/// </summary>
public class StorageQueueMessagingBuilder
{
    internal StorageQueueMessagingBuilder(
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
    public StorageQueueMessagingBuilder Configure(
        Action<StorageQueueMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(ConnectionName, configure);
        return this;
    }

    /// <summary>
    /// Binds the connection options to a configuration section, e.g. one with a
    /// <c>QueueServiceUri</c> or <c>ConnectionString</c> value.
    /// </summary>
    /// <param name="configuration">The configuration section to bind the options to.</param>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingBuilder Configure(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Services.Configure<StorageQueueMessagingOptions>(ConnectionName, configuration);
        return this;
    }

    /// <summary>
    /// Configures the connection options with an <see cref="IConfigureOptions{TOptions}"/> resolved from DI.
    /// </summary>
    /// <typeparam name="TConfigureOptions">The type that configures the options, resolved from dependency injection.</typeparam>
    /// <returns>The same instance, for chaining.</returns>
    public StorageQueueMessagingBuilder Configure<TConfigureOptions>()
        where TConfigureOptions : class, IConfigureOptions<StorageQueueMessagingOptions>
    {
        Services.ConfigureOptions<TConfigureOptions>();
        return this;
    }

    /// <summary>
    /// Registers an <see cref="IStorageQueuePublisher{TMessage}"/> and <see cref="IMessagePublisher{TMessage}"/>
    /// that publish <typeparamref name="TMessage"/> to <paramref name="queueName"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="configure">Optionally configures the publisher.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Publishers on a named connection are registered as keyed services under the connection name. They are also
    /// registered without a key, unless a publisher for the same message type exists on the default connection.
    /// </remarks>
    public StorageQueueMessagingBuilder AddPublisher<TMessage>(
        string queueName,
        Action<StorageQueuePublisherBuilder<TMessage>>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var publisherBuilder = new StorageQueuePublisherBuilder<TMessage>();
        configure?.Invoke(publisherBuilder);

        Services.AddStorageQueueCoreServices();
        Services.TryAddSingleton<IStorageQueuePublisherFactory, StorageQueuePublisherFactory>();
        Services.AddSingleton(new StorageQueuePublisherRegistration(typeof(TMessage), ConnectionName, queueName, publisherBuilder));

        var connectionName = ConnectionName;
        Services.AddKeyedSingleton<IStorageQueuePublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredService<IStorageQueuePublisherFactory>().Create<TMessage>(connectionName));
        Services.AddKeyedSingleton<IMessagePublisher<TMessage>>(
            connectionName,
            (s, _) => s.GetRequiredKeyedService<IStorageQueuePublisher<TMessage>>(connectionName));

        if (connectionName is not null)
        {
            Services.TryAddSingleton(s => s.GetRequiredKeyedService<IStorageQueuePublisher<TMessage>>(connectionName));
            Services.TryAddSingleton<IMessagePublisher<TMessage>>(s => s.GetRequiredKeyedService<IStorageQueuePublisher<TMessage>>(connectionName));
        }

        return this;
    }

    /// <summary>
    /// Registers a hosted processor that polls <paramref name="queueName"/>,
    /// handing <typeparamref name="TMessage"/> messages to <typeparamref name="TProcessor"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="configure">Optionally configures the processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Processed messages are deleted. Messages that cannot be deserialized, or that failed the configured number of
    /// times, are moved to the poison queue. Other failed messages are received again once their visibility timeout expires.
    /// </para>
    /// <para>
    /// <typeparamref name="TProcessor"/> is registered as a singleton unless it is already registered. It is resolved
    /// from a new scope for every message, so register it as scoped to get scoped dependencies per message.
    /// </para>
    /// </remarks>
    public StorageQueueMessagingBuilder AddProcessor<TMessage, TProcessor>(
        string queueName,
        Action<StorageQueueProcessorBuilder>? configure = null)
        where TProcessor : class, IMessageProcessor<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        var processorBuilder = new StorageQueueProcessorBuilder(queueName);
        configure?.Invoke(processorBuilder);

        Services.AddStorageQueueCoreServices();
        Services.AddLogging();
        Services.TryAddSingleton<TProcessor>();

        var connectionName = ConnectionName;
        var key = new object();
        Services.AddKeyedSingleton(
            key,
            (s, _) =>
            {
                var clientFactory = s.GetRequiredService<IStorageQueueClientFactory>();
                var options = clientFactory.GetOptions(connectionName);

                return new StorageQueueProcessorService<TMessage, TProcessor>(
                    clientFactory.GetQueueClient(connectionName, queueName),
                    clientFactory.GetQueueClient(connectionName, processorBuilder.PoisonQueueName),
                    new MessageDispatcher<TMessage, TProcessor, StorageQueueMetadata>(
                        s.GetRequiredService<IServiceScopeFactory>(),
                        options.Serializer,
                        [],
                        s.GetRequiredService<ILogger<TProcessor>>()),
                    new StorageQueueProcessorSettings(
                        processorBuilder.PollingInterval,
                        processorBuilder.MaxMessages,
                        processorBuilder.VisibilityTimeout,
                        processorBuilder.MaxDequeueCount,
                        processorBuilder.CreateQueuesIfNotExist),
                    s.GetService<TimeProvider>() ?? TimeProvider.System,
                    s.GetRequiredService<TProcessor>);
            });
        Services.AddSingleton<IHostedService>(s => s.GetRequiredKeyedService<StorageQueueProcessorService<TMessage, TProcessor>>(key));
        Services.AddSingleton<IMessageProcessorService<TProcessor>>(s => s.GetRequiredKeyedService<StorageQueueProcessorService<TMessage, TProcessor>>(key));

        return this;
    }
}