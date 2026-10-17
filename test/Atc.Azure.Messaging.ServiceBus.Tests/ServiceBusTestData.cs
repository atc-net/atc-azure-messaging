namespace Atc.Azure.Messaging.ServiceBus.Tests;

internal static class ServiceBusTestData
{
    public static readonly DateTimeOffset EnqueuedTime = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    public static ServiceBusReceivedMessage CreateMessage(
        TestMessage message,
        int deliveryCount = 1)
        => CreateMessage(TestDispatcher.Serialize(message), message.Id, deliveryCount);

    public static ServiceBusReceivedMessage CreateMessage(
        BinaryData body,
        string messageId,
        int deliveryCount = 1)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: body,
            messageId: messageId,
            partitionKey: "session-1",
            sessionId: "session-1",
            replyToSessionId: "reply-session",
            timeToLive: TimeSpan.FromMinutes(5),
            correlationId: $"correlation-{messageId}",
            subject: "test-subject",
            to: "to",
            contentType: "application/json",
            replyTo: "reply-to",
            scheduledEnqueueTime: EnqueuedTime.AddMinutes(-1),
            properties: new Dictionary<string, object>(StringComparer.Ordinal) { ["TenantId"] = "tenant" },
            lockTokenGuid: Guid.NewGuid(),
            deliveryCount: deliveryCount,
            lockedUntil: EnqueuedTime.AddMinutes(1),
            sequenceNumber: 42,
            deadLetterSource: null,
            enqueuedSequenceNumber: 41,
            enqueuedTime: EnqueuedTime,
            serviceBusMessageState: ServiceBusMessageState.Active);
}