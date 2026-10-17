namespace Atc.Azure.Messaging.EventHub.Builders;

/// <summary>
/// Configures the defaults applied to every message published by an Event Hub publisher.
/// </summary>
/// <typeparam name="TMessage">The type of message published.</typeparam>
public class EventHubPublisherBuilder<TMessage>
{
    private readonly List<Action<TMessage, EventData>> modifiers = [];

    internal Func<TMessage, string>? PartitionKeySelector { get; private set; }

    /// <summary>
    /// Sets the partition key of each event from the message, so related events end up on the same partition.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    public EventHubPublisherBuilder<TMessage> WithPartitionKey(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        PartitionKeySelector = selector;
        return this;
    }

    /// <summary>
    /// Publishes every event with the same partition key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    public EventHubPublisherBuilder<TMessage> WithPartitionKey(
        string partitionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        PartitionKeySelector = _ => partitionKey;
        return this;
    }

    public EventHubPublisherBuilder<TMessage> WithMessageId(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, e) => e.MessageId = selector(m));
        return this;
    }

    public EventHubPublisherBuilder<TMessage> WithCorrelationId(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, e) => e.CorrelationId = selector(m));
        return this;
    }

    /// <summary>
    /// Adds an application property whose value is taken from the message.
    /// </summary>
    /// <param name="name">The name of the application property.</param>
    /// <param name="selector">Selects the property value from the message.</param>
    public EventHubPublisherBuilder<TMessage> WithProperty(
        string name,
        Func<TMessage, object?> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, e) => e.Properties[name] = selector(m));
        return this;
    }

    /// <summary>
    /// Adds an application property with a fixed value.
    /// </summary>
    /// <param name="name">The name of the application property.</param>
    /// <param name="value">The value of the application property.</param>
    public EventHubPublisherBuilder<TMessage> WithProperty(
        string name,
        object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        modifiers.Add((_, e) => e.Properties[name] = value);
        return this;
    }

    /// <summary>
    /// Adds an application property named after the selected member, e.g. <c>m =&gt; m.TenantId</c>.
    /// </summary>
    /// <param name="selector">Selects the property value from the message.</param>
    public EventHubPublisherBuilder<TMessage> WithProperty(
        Expression<Func<TMessage, object?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return WithProperty(PropertyNameResolver.GetName(selector), selector.Compile());
    }

    internal void Apply(
        TMessage message,
        EventData eventData)
    {
        foreach (var modifier in modifiers)
        {
            modifier(message, eventData);
        }
    }
}