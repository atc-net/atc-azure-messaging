namespace Atc.Azure.Messaging.ServiceBus.Publishing.Internal;

internal sealed record ServiceBusPublisherRegistration(
    Type MessageType,
    string? ConnectionName,
    string TopicOrQueueName,
    object Builder);