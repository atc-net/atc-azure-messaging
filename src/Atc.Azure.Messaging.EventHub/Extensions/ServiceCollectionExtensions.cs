// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds an Event Hubs connection, configured through <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    /// <example>
    /// <code>
    /// services.AddEventHubMessaging(eh => eh
    ///     .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    ///     .AddPublisher&lt;OrderPlaced&gt;("orders", p => p.WithPartitionKey(m => m.CustomerId)));
    /// </code>
    /// </example>
    public static IServiceCollection AddEventHubMessaging(
        this IServiceCollection services,
        Action<EventHubMessagingBuilder> configure)
        => services.AddEventHubMessaging(connectionName: null, configure);

    /// <summary>
    /// Adds a named Event Hubs connection, configured through <paramref name="configure"/>.
    /// Use named connections to talk to more than one Event Hubs namespace.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="connectionName">The name of the connection, or <see langword="null"/> for the default connection.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddEventHubMessaging(
        this IServiceCollection services,
        string? connectionName,
        Action<EventHubMessagingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddEventHubCoreServices();
        services.AddOptions<EventHubMessagingOptions>(connectionName);

        configure(new EventHubMessagingBuilder(services, connectionName));

        return services;
    }

    internal static IServiceCollection AddEventHubCoreServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IEventHubClientFactory, EventHubClientFactory>();
        return services;
    }
}