namespace Atc.Azure.Messaging.EventHub.Publishing.Internal;

internal sealed record EventHubPublisherRegistration(
    Type MessageType,
    string? ConnectionName,
    string EventHubName,
    object Builder);