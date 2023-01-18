using System.Diagnostics.CodeAnalysis;
using Atc.Azure.Options.Authorization;
using Atc.Azure.Options.Environment;
using Atc.Azure.Options.EventHub;
using Atc.Azure.Options.Providers;
using Azure.Identity;
using Azure.Messaging.EventHubs.Producer;

namespace Atc.Azure.Messaging.EventHub;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "EventHubPublisher is responsible for disposing EventHubProducerClient")]
internal sealed class EventHubCredentialsPublisherFactory : IEventHubPublisherFactory
{
    private readonly string fullyQualifiedNamespace;
    private readonly DefaultAzureCredentialOptions credentialOptions;

    public EventHubCredentialsPublisherFactory(
        EventHubOptions options,
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

    public IEventHubPublisher Create(string eventHubName)
        => new EventHubPublisher(
                new EventHubProducerClient(
                    fullyQualifiedNamespace,
                    eventHubName,
                    new DefaultAzureCredential(credentialOptions)));
}