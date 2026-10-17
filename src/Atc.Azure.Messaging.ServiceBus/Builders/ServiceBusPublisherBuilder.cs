namespace Atc.Azure.Messaging.ServiceBus.Builders;

/// <summary>
/// Configures the defaults applied to every message published by a Service Bus publisher.
/// </summary>
/// <typeparam name="TMessage">The type of message published.</typeparam>
public class ServiceBusPublisherBuilder<TMessage>
{
    private readonly List<Action<TMessage, ServiceBusMessage>> modifiers = [];

    internal ServiceBusSenderOptions? SenderOptions { get; private set; }

    /// <summary>
    /// Sets the message id from the message, e.g. to enable duplicate detection.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithMessageId(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.MessageId = selector(m));
        return this;
    }

    /// <summary>
    /// Sets the correlation id from the message.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithCorrelationId(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.CorrelationId = selector(m));
        return this;
    }

    /// <summary>
    /// Sets the session id from the message. Required when publishing to a session enabled entity.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithSessionId(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.SessionId = selector(m));
        return this;
    }

    /// <summary>
    /// Sets the partition key from the message, for partitioned entities.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithPartitionKey(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.PartitionKey = selector(m));
        return this;
    }

    /// <summary>
    /// Sets the subject (label) from the message, e.g. for subscription rules.
    /// </summary>
    /// <param name="selector">Selects the value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithSubject(
        Func<TMessage, string> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.Subject = selector(m));
        return this;
    }

    /// <summary>
    /// Sets the same subject (label) on every message, e.g. for subscription rules.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        modifiers.Add((_, sb) => sb.Subject = subject);
        return this;
    }

    /// <summary>
    /// Sets how long every message lives before it expires.
    /// </summary>
    /// <param name="timeToLive">The time to live.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithTimeToLive(
        TimeSpan timeToLive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        modifiers.Add((_, sb) => sb.TimeToLive = timeToLive);
        return this;
    }

    /// <summary>
    /// Adds an application property whose value is taken from the message.
    /// </summary>
    /// <param name="name">The name of the application property.</param>
    /// <param name="selector">Selects the property value from the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithProperty(
        string name,
        Func<TMessage, object?> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(selector);
        modifiers.Add((m, sb) => sb.ApplicationProperties[name] = selector(m));
        return this;
    }

    /// <summary>
    /// Adds an application property with a fixed value.
    /// </summary>
    /// <param name="name">The name of the application property.</param>
    /// <param name="value">The value of the application property.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithProperty(
        string name,
        object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        modifiers.Add((_, sb) => sb.ApplicationProperties[name] = value);
        return this;
    }

    /// <summary>
    /// Adds an application property named after the selected member, e.g. <c>m =&gt; m.TenantId</c>.
    /// </summary>
    /// <param name="selector">Selects the property or field of the message.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithProperty(
        Expression<Func<TMessage, object?>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return WithProperty(PropertyNameResolver.GetName(selector), selector.Compile());
    }

    /// <summary>
    /// Sets the options used when creating the sender, e.g. a transaction group.
    /// </summary>
    /// <param name="senderOptions">The options used when creating the sender.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusPublisherBuilder<TMessage> WithSenderOptions(
        ServiceBusSenderOptions senderOptions)
    {
        ArgumentNullException.ThrowIfNull(senderOptions);
        SenderOptions = senderOptions;
        return this;
    }

    internal void Apply(
        TMessage message,
        ServiceBusMessage serviceBusMessage)
    {
        foreach (var modifier in modifiers)
        {
            modifier(message, serviceBusMessage);
        }
    }
}