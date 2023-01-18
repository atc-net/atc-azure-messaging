using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Options.Authorization;
using Atc.Azure.Options.Environment;
using Atc.Azure.Options.EventHub;
using Atc.Azure.Options.Providers;

namespace Atc.Azure.Messaging.Tests.EventHub;

public class EventHubCredentialsPublisherFactoryTests
{
    private const string FullyQualifiedNamespace = "eventHubNamespace.servicebus.windows.net";

    [Theory, AutoNSubstituteData]
    public void Create_Returns_NotNull(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions,
        string eventHubName)
        => new EventHubCredentialsPublisherFactory(
            new EventHubOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider)
        .Create(eventHubName)
        .Should()
        .NotBeNull();

    [Theory, AutoNSubstituteData]
    public void Create_Returns_IEventHubPublisher(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions,
        string eventHubName)
        => new EventHubCredentialsPublisherFactory(
            new EventHubOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider)
        .Create(eventHubName)
        .Should()
        .BeAssignableTo<IEventHubPublisher>();

    [Theory, AutoNSubstituteData]
    public void Constructor_Calls_AzureCredentialsOptionsProvider(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        EventHubOptions eventHubOptions,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions)
    {
        _ = new EventHubCredentialsPublisherFactory(
            eventHubOptions,
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider);

        credentialOptionsProvider
            .Received(1)
            .GetAzureCredentialOptions(environmentOptions, authorizationOptions);
    }
}