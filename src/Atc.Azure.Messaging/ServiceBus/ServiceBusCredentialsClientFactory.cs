using Atc.Azure.Options.Authorization;
using Atc.Azure.Options.Environment;
using Atc.Azure.Options.Providers;
using Atc.Azure.Options.ServiceBus;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public class ServiceBusCredentialsClientFactory : IServiceBusClientFactory
{
    private readonly string fullyQualifiedNamespace;
    private readonly DefaultAzureCredentialOptions credentialOptions;

    public ServiceBusCredentialsClientFactory(
        ServiceBusOptions options,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions clientCredentialOptions,
        IAzureCredentialOptionsProvider credentialOptionsProvider)
    {
        this.fullyQualifiedNamespace = options.FullyQualifiedNamespace;
        this.credentialOptions = credentialOptionsProvider
            .GetAzureCredentialOptions(
                environmentOptions,
                clientCredentialOptions);
    }

    public ServiceBusClient Create() => new ServiceBusClient(
        fullyQualifiedNamespace,
        new DefaultAzureCredential(credentialOptions));
}