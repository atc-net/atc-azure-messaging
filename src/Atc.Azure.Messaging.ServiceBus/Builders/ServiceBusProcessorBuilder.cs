namespace Atc.Azure.Messaging.ServiceBus.Builders;

/// <summary>
/// Configures a Service Bus processor for a queue or subscription.
/// </summary>
public class ServiceBusProcessorBuilder
{
    internal List<Func<ServiceBusMetadata, bool>> Filters { get; } = [];

    internal ServiceBusProcessorOptions? ProcessorOptions { get; private set; }

    /// <summary>
    /// Only hands messages matching <paramref name="predicate"/> to the processor. Other messages are completed
    /// without processing. Multiple filters must all match.
    /// </summary>
    /// <param name="predicate">The condition a message must match to be processed.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Prefer subscription rules on the broker when possible, so unwanted messages are never delivered.
    /// </remarks>
    public ServiceBusProcessorBuilder WithFilter(
        Func<ServiceBusMetadata, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// Sets the options of the underlying <see cref="ServiceBusProcessor"/>, e.g. concurrency, prefetch,
    /// or <see cref="ServiceBusProcessorOptions.SubQueue"/> to process the dead-letter queue.
    /// </summary>
    /// <param name="options">The options passed to the underlying processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Messages are always settled explicitly, so <see cref="ServiceBusProcessorOptions.AutoCompleteMessages"/> is ignored.
    /// </remarks>
    public ServiceBusProcessorBuilder WithProcessorOptions(
        ServiceBusProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ProcessorOptions = options;
        return this;
    }
}