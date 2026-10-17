namespace Atc.Azure.Messaging.StorageQueue.Tests;

internal static class StorageQueueTestData
{
    public static readonly DateTimeOffset InsertedOn = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    public static QueueMessage CreateMessage(
        TestMessage message,
        long dequeueCount = 1)
        => CreateMessage(TestDispatcher.Serialize(message), message.Id, dequeueCount);

    public static QueueMessage CreateMessage(
        BinaryData body,
        string messageId,
        long dequeueCount = 1)
        => QueuesModelFactory.QueueMessage(
            messageId: messageId,
            popReceipt: $"receipt-{messageId}",
            body: body,
            dequeueCount: dequeueCount,
            nextVisibleOn: InsertedOn.AddSeconds(30),
            insertedOn: InsertedOn,
            expiresOn: InsertedOn.AddDays(7));
}