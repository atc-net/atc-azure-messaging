namespace Atc.Azure.Messaging.EventHub.Builders;

/// <summary>
/// Configures a stateless Event Hub processor, which reads every partition without checkpointing.
/// </summary>
public class EventHubStatelessProcessorBuilder
{
    internal List<Func<EventHubMetadata, bool>> Filters { get; } = [];

    internal ReadEventOptions? ReadOptions { get; private set; }

    internal EventPosition StartingPosition { get; private set; } = EventPosition.Latest;

    /// <summary>
    /// Only hands events matching <paramref name="predicate"/> to the processor. Multiple filters must all match.
    /// </summary>
    /// <param name="predicate">The condition a message must match to be processed.</param>
    public EventHubStatelessProcessorBuilder WithFilter(
        Func<EventHubMetadata, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    public EventHubStatelessProcessorBuilder WithReadOptions(
        ReadEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ReadOptions = options;
        return this;
    }

    /// <summary>
    /// Sets where reading starts when the processor starts. Defaults to <see cref="EventPosition.Latest"/>.
    /// </summary>
    /// <param name="position">The position to start reading from.</param>
    public EventHubStatelessProcessorBuilder WithStartingPosition(
        EventPosition position)
    {
        StartingPosition = position;
        return this;
    }
}