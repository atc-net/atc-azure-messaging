namespace Atc.Azure.Messaging.EventHub.Tests;

internal static class EventHubTestData
{
    public static EventData CreateEvent(
        TestMessage message,
        long sequenceNumber = 1)
    {
        var eventData = EventHubsModelFactory.EventData(
            eventBody: TestDispatcher.Serialize(message),
            properties: new Dictionary<string, object>(StringComparer.Ordinal) { ["TenantId"] = message.TenantId },
            systemProperties: new Dictionary<string, object>(StringComparer.Ordinal) { ["x-opt-system"] = "value" },
            partitionKey: message.TenantId,
            sequenceNumber: sequenceNumber,
            offsetString: $"{sequenceNumber * 100}",
            enqueuedTime: new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        eventData.MessageId = message.Id;
        eventData.CorrelationId = $"correlation-{message.Id}";
        eventData.ContentType = "application/json";

        return eventData;
    }

    public static PartitionEvent CreatePartitionEvent(
        string partitionId,
        EventData eventData)
        => new(
            EventHubsModelFactory.PartitionContext("test.servicebus.windows.net", "hub", "$Default", partitionId),
            eventData);
}