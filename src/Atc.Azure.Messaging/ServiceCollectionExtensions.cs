using System.Diagnostics.CodeAnalysis;
using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Messaging.ServiceBus;
using Atc.Azure.Options.Authorization;
using Atc.Azure.Options.Environment;
using Atc.Azure.Options.EventHub;
using Atc.Azure.Options.Providers;
using Atc.Azure.Options.ServiceBus;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds messaging services from the configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configurations</param>
    /// <param name="useAzureCredentials">
    /// Flag indicating whether to use managed identity with azure credentials or
    /// connection strings for the messaging service configuration.
    /// <para>
    /// Defaults to using connection strings.
    /// </para>
    /// </param>
    /// <param name="credentialOptionsProvider">
    /// The <see cref="IAzureCredentialOptionsProvider"/> to be used for configuring
    /// the <see cref="DefaultAzureCredential"/>.
    /// <para>
    /// Defaults to <see cref="AzureCredentialOptionsProvider"/> if
    /// <paramref name="credentialOptionsProvider"/> is null.
    /// </para>
    /// <para>
    /// Parameter is only considered if <paramref name="useAzureCredentials"/> is set to true.
    /// </para>
    /// </param>
    public static void ConfigureMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useAzureCredentials = false,
        IAzureCredentialOptionsProvider? credentialOptionsProvider = null)
    {
        services.AddOptions<EventHubOptions>(configuration);
        services.AddOptions<ServiceBusOptions>(configuration);

        if (useAzureCredentials)
        {
            services.AddOptions<EnvironmentOptions>(configuration);
            services.AddOptions<ClientAuthorizationOptions>(configuration);

            credentialOptionsProvider ??= new AzureCredentialOptionsProvider();
            services.AddSingleton<IAzureCredentialOptionsProvider>(credentialOptionsProvider);

            services.AddSingleton<IEventHubPublisherFactory, EventHubCredentialsPublisherFactory>();
            services.AddSingleton<IServiceBusClientFactory, ServiceBusCredentialsClientFactory>();
        }

        services.TryAddSingleton<IEventHubPublisherFactory, EventHubPublisherFactory>();
        services.TryAddSingleton<IServiceBusClientFactory, ServiceBusClientFactory>();
        services.AddSingleton<IServiceBusSenderProvider, ServiceBusSenderProvider>();
        services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
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