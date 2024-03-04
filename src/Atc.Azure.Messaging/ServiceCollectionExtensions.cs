using Atc.Azure.Messaging.Serialization;

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
    /// <param name="jsonSerializerOptions">
    /// Optional <see cref="JsonSerializerOptions"/> to customize the serialization settings
    /// used by messaging components for serializing and deserializing message payloads.
    /// <para>
    /// Provides a way to specify custom serialization behavior.
    /// When not provided, the default serialization settings of <see cref="System.Text.Json.JsonSerializer"/> are used.
    /// </para>
    /// </param>
    public static void ConfigureMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useAzureCredentials = false,
        IAzureCredentialOptionsProvider? credentialOptionsProvider = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
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

        services.AddSingleton<IMessagePayloadSerializer>(
            new MessagePayloadSerializer(
                jsonSerializerOptions ?? new JsonSerializerOptions()));
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