namespace Atc.Azure.Messaging.Tests.Common;

/// <summary>
/// A processor that records which instance handled each message, to verify per-message scopes.
/// </summary>
public sealed class ScopedTestProcessor : IMessageProcessor<TestMessage>
{
    private static readonly System.Collections.Concurrent.ConcurrentBag<Guid> Instances = [];

    public Guid InstanceId { get; } = Guid.NewGuid();

    public static IReadOnlyCollection<Guid> HandledBy => [.. Instances];

    public static void Reset() => Instances.Clear();

    public Task ProcessAsync(
        TestMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        Instances.Add(InstanceId);
        return Task.CompletedTask;
    }
}