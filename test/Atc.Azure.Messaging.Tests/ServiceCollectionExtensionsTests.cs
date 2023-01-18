using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Messaging.ServiceBus;
using Atc.Azure.Options.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atc.Azure.Messaging.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection services = new ServiceCollection();

    [Theory]
    [InlineAutoNSubstituteData(false, typeof(IEventHubPublisherFactory), typeof(EventHubPublisherFactory))]
    [InlineAutoNSubstituteData(false, typeof(IServiceBusClientFactory), typeof(ServiceBusClientFactory))]
    [InlineAutoNSubstituteData(false, typeof(IServiceBusPublisher), typeof(ServiceBusPublisher))]
    [InlineAutoNSubstituteData(false, typeof(IServiceBusSenderProvider), typeof(ServiceBusSenderProvider))]
    [InlineAutoNSubstituteData(true, typeof(IEventHubPublisherFactory), typeof(EventHubCredentialsPublisherFactory))]
    [InlineAutoNSubstituteData(true, typeof(IServiceBusClientFactory), typeof(ServiceBusCredentialsClientFactory))]
    [InlineAutoNSubstituteData(true, typeof(IServiceBusPublisher), typeof(ServiceBusPublisher))]
    [InlineAutoNSubstituteData(true, typeof(IServiceBusSenderProvider), typeof(ServiceBusSenderProvider))]
    public void ConfigureMessagingServices_Adds_Messaging_Dependencies(
        bool useAzureCredentials,
        Type serviceType,
        Type implementationType,
        [Frozen] IConfiguration configuration)
    {
        services.ConfigureMessagingServices(configuration, useAzureCredentials);

        services
            .Should()
            .ContainSingle(s =>
                s.Lifetime == ServiceLifetime.Singleton &&
                s.ServiceType == serviceType &&
                s.ImplementationType == implementationType);
    }

    [Theory, AutoNSubstituteData]
    public void ConfigureMessagingServices_Adds_AzureCredentialOptionsProvider_Default_Dependency(
        [Frozen] IConfiguration configuration)
    {
        services.ConfigureMessagingServices(configuration, true, null);

        services
            .Should()
            .ContainSingle(s =>
                s.Lifetime == ServiceLifetime.Singleton &&
                s.ServiceType == typeof(IAzureCredentialOptionsProvider))
            .Which
            .ImplementationInstance
            .Should()
            .BeOfType<AzureCredentialOptionsProvider>();
    }

    [Theory, AutoNSubstituteData]
    public void ConfigureMessagingServices_Adds_AzureCredentialOptionsProvider_Instance_Dependency(
        [Frozen] IConfiguration configuration)
    {
        var azureCredentialOptionsProviderInstance = new AzureCredentialOptionsProvider();

        services.ConfigureMessagingServices(configuration, true, azureCredentialOptionsProviderInstance);

        services
            .Should()
            .ContainSingle(s =>
                s.Lifetime == ServiceLifetime.Singleton &&
                s.ServiceType == typeof(IAzureCredentialOptionsProvider))
            .Which
            .ImplementationInstance
            .Should()
            .BeSameAs(azureCredentialOptionsProviderInstance);
    }
}
