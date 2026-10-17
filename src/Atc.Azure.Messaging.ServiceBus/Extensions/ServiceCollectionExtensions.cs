// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Service Bus connection, configured through <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    /// <example>
    /// <code>
    /// services.AddServiceBusMessaging(sb => sb
    ///     .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    ///     .AddPublisher&lt;OrderPlaced&gt;("orders")
    ///     .AddProcessor&lt;OrderPlaced, OrderPlacedProcessor&gt;("orders", "billing"));
    /// </code>
    /// </example>
    public static IServiceCollection AddServiceBusMessaging(
        this IServiceCollection services,
        Action<ServiceBusMessagingBuilder> configure)
        => services.AddServiceBusMessaging(connectionName: null, configure);

    /// <summary>
    /// Adds a named Service Bus connection, configured through <paramref name="configure"/>.
    /// Use named connections to talk to more than one Service Bus namespace.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="connectionName">The name of the connection, or <see langword="null"/> for the default connection.</param>
    /// <param name="configure">Configures the connection, its publishers and processors.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddServiceBusMessaging(
        this IServiceCollection services,
        string? connectionName,
        Action<ServiceBusMessagingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddServiceBusCoreServices();
        services.AddOptions<ServiceBusMessagingOptions>(connectionName);

        configure(new ServiceBusMessagingBuilder(services, connectionName));

        return services;
    }

    internal static IServiceCollection AddServiceBusCoreServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IServiceBusClientFactory, ServiceBusClientFactory>();
        return services;
    }
}