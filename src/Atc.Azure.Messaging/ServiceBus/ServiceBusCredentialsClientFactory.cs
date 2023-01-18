namespace Atc.Azure.Messaging.ServiceBus;

internal sealed class ServiceBusCredentialsClientFactory : IServiceBusClientFactory
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