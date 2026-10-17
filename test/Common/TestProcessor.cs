namespace Atc.Azure.Messaging.Tests.Common;

/// <summary>
/// Records received messages, and throws for messages with the id <c>fail</c>.
/// </summary>
public sealed class TestProcessor : IMessageProcessor<TestMessage>
{
    public const string FailingMessageId = "fail";

    private readonly List<(TestMessage Message, MessageMetadata Metadata)> received = [];

    public IReadOnlyList<(TestMessage Message, MessageMetadata Metadata)> Received
    {
        get
        {
            lock (received)
            {
                return [.. received];
            }
        }
    }

    public Func<TestMessage, CancellationToken, Task>? OnProcess { get; set; }

    public async Task ProcessAsync(
        TestMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (OnProcess is not null)
        {
            await OnProcess(message, cancellationToken);
        }

        if (message.Id == FailingMessageId)
        {
            throw new InvalidOperationException("Processing failed.");
        }

        lock (received)
        {
            received.Add((message, metadata));
        }
    }
}