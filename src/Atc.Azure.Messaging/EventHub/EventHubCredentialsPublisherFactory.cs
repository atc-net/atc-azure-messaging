using Atc.Azure.Messaging.Serialization;

namespace Atc.Azure.Messaging.EventHub;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "EventHubPublisher is responsible for disposing EventHubProducerClient")]
internal sealed class EventHubCredentialsPublisherFactory : IEventHubPublisherFactory
{
    private readonly string fullyQualifiedNamespace;
    private readonly DefaultAzureCredentialOptions credentialOptions;
    private readonly IMessagePayloadSerializer messagePayloadSerializer;

    public EventHubCredentialsPublisherFactory(
        EventHubOptions options,
        EnvironmentOptions environmentOptions,
        ClientAuthorizationOptions clientCredentialOptions,
        IAzureCredentialOptionsProvider credentialOptionsProvider,
        IMessagePayloadSerializer messagePayloadSerializer)
    {
        this.messagePayloadSerializer = messagePayloadSerializer;
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
                new DefaultAzureCredential(credentialOptions)),
            messagePayloadSerializer);
}