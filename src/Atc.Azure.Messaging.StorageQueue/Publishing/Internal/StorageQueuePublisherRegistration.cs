namespace Atc.Azure.Messaging.StorageQueue.Publishing.Internal;

internal sealed record StorageQueuePublisherRegistration(
    Type MessageType,
    string? ConnectionName,
    string QueueName,
    object Builder);