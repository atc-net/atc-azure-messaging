namespace Atc.Azure.Messaging.Tests.Common;

/// <summary>
/// A processor that always fails and records the errors it is told about.
/// </summary>
public sealed class TestErrorHandlingProcessor : IMessageProcessor<TestMessage>, IProcessErrorHandler
{
    private readonly List<(Exception Exception, MessageMetadata? Metadata)> errors = [];

    public IReadOnlyList<(Exception Exception, MessageMetadata? Metadata)> Errors
    {
        get
        {
            lock (errors)
            {
                return [.. errors];
            }
        }
    }

    public Task ProcessAsync(
        TestMessage message,
        MessageMetadata metadata,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException($"Processing {message.Id} failed.");

    public Task ProcessErrorAsync(
        Exception exception,
        MessageMetadata? metadata,
        CancellationToken cancellationToken)
    {
        lock (errors)
        {
            errors.Add((exception, metadata));
        }

        return Task.CompletedTask;
    }
}