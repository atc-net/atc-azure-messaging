using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.Tests.EventHub;

public class EventHubCredentialsPublisherFactoryTests
{
    private const string FullyQualifiedNamespace = "eventHubNamespace.servicebus.windows.net";

    [Theory, AutoNSubstituteData]
    public void Create_Returns_NotNull(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        IMessagePayloadSerializer messagePayloadSerializer,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions,
        string eventHubName)
        => new EventHubCredentialsPublisherFactory(
            new EventHubOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider,
            messagePayloadSerializer)
        .Create(eventHubName)
        .Should()
        .NotBeNull();

    [Theory, AutoNSubstituteData]
    public void Create_Returns_IEventHubPublisher(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        IMessagePayloadSerializer messagePayloadSerializer,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions,
        string eventHubName)
        => new EventHubCredentialsPublisherFactory(
            new EventHubOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider,
            messagePayloadSerializer)
        .Create(eventHubName)
        .Should()
        .BeAssignableTo<IEventHubPublisher>();

    [Theory, AutoNSubstituteData]
    public void Constructor_Calls_AzureCredentialsOptionsProvider(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        IMessagePayloadSerializer messagePayloadSerializer,
        EventHubOptions eventHubOptions,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions)
    {
        _ = new EventHubCredentialsPublisherFactory(
            eventHubOptions,
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider,
            messagePayloadSerializer);

        credentialOptionsProvider
            .Received(1)
            .GetAzureCredentialOptions(environmentOptions, authorizationOptions);
    }
}