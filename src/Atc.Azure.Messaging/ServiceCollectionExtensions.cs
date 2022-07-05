using System.Diagnostics.CodeAnalysis;
using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Messaging.ServiceBus;
using Atc.Azure.Options.EventHub;
using Atc.Azure.Options.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EventHubOptions>(configuration);
        services.AddOptions<ServiceBusOptions>(configuration);

        services.AddSingleton<IEventHubPublisherFactory, EventHubPublisherFactory>();
        services.AddSingleton<IServiceBusClientFactory, ServiceBusClientFactory>();
        services.AddSingleton<IServiceBusSenderProvider, ServiceBusSenderProvider>();
        services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

        return services;
    }

    private static T AddOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration)
        where T : class, new()
    {
        var options = Activator.CreateInstance<T>();

        configuration.GetSection(options.GetType().Name).Bind(options);
        services.AddSingleton(options);

        return options;
    }
}