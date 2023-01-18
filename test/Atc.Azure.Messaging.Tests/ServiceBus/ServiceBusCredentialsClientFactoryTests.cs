using Atc.Azure.Messaging.ServiceBus;
using Atc.Azure.Options.Authorization;
using Atc.Azure.Options.Environment;
using Atc.Azure.Options.Providers;
using Atc.Azure.Options.ServiceBus;

namespace Atc.Azure.Messaging.Tests.ServiceBus;

public class ServiceBusCredentialsClientFactoryTests
{
    private const string FullyQualifiedNamespace = "serviceBusNamespace.servicebus.windows.net";

    [Theory, AutoNSubstituteData]
    public void Create_Returns_Not_Null(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions) =>
        new ServiceBusCredentialsClientFactory(
            new ServiceBusOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider)
        .Create()
        .Should()
        .NotBeNull();

    [Theory, AutoNSubstituteData]
    public void Create_Returns_Client_With_FullyQualifiedNamespace(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions) =>
        new ServiceBusCredentialsClientFactory(
            new ServiceBusOptions { FullyQualifiedNamespace = FullyQualifiedNamespace },
            environmentOptions,
            authorizationOptions,
            credentialOptionsProvider)
        .Create()
        .FullyQualifiedNamespace
        .Should()
        .Be(FullyQualifiedNamespace);

    [Theory, AutoNSubstituteData]
    public void Constructor_Calls_AzureCredentialsOptionsProvider(
        [Frozen] IAzureCredentialOptionsProvider credentialOptionsProvider,
        ServiceBusOptions serviceBusOptions,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions authorizationOptions)
    {
        _ = new ServiceBusCredentialsClientFactory(
              serviceBusOptions,
              environmentOptions,
              authorizationOptions,
              credentialOptionsProvider);

        credentialOptionsProvider
            .Received(1)
            .GetAzureCredentialOptions(environmentOptions, authorizationOptions);
    }
}