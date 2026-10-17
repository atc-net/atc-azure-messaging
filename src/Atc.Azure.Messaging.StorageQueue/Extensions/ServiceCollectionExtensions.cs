// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Storage Queue connection, configured through <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    /// <example>
    /// <code>
    /// services.AddStorageQueueMessaging(sq => sq
    ///     .Configure(o => o.WithConnection(new Uri("https://account.queue.core.windows.net"), new DefaultAzureCredential()))
    ///     .AddPublisher&lt;ResizeImage&gt;("images")
    ///     .AddProcessor&lt;ResizeImage, ResizeImageProcessor&gt;("images"));
    /// </code>
    /// </example>
    public static IServiceCollection AddStorageQueueMessaging(
        this IServiceCollection services,
        Action<StorageQueueMessagingBuilder> configure)
        => services.AddStorageQueueMessaging(connectionName: null, configure);

    /// <summary>
    /// Adds a named Storage Queue connection, configured through <paramref name="configure"/>.
    /// Use named connections to talk to more than one storage account.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="connectionName">The name of the connection, or <see langword="null"/> for the default connection.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddStorageQueueMessaging(
        this IServiceCollection services,
        string? connectionName,
        Action<StorageQueueMessagingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddStorageQueueCoreServices();
        services.AddOptions<StorageQueueMessagingOptions>(connectionName);

        configure(new StorageQueueMessagingBuilder(services, connectionName));

        return services;
    }

    internal static IServiceCollection AddStorageQueueCoreServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IStorageQueueClientFactory, StorageQueueClientFactory>();
        return services;
    }
}