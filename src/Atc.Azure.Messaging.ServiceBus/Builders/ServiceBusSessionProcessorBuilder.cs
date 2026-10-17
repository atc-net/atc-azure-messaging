namespace Atc.Azure.Messaging.ServiceBus.Builders;

/// <summary>
/// Configures a Service Bus session processor for a session enabled queue or subscription.
/// </summary>
public class ServiceBusSessionProcessorBuilder
{
    internal List<Func<ServiceBusMetadata, bool>> Filters { get; } = [];

    internal ServiceBusSessionProcessorOptions? ProcessorOptions { get; private set; }

    /// <summary>
    /// Only hands messages matching <paramref name="predicate"/> to the processor. Other messages are completed
    /// without processing. Multiple filters must all match.
    /// </summary>
    /// <param name="predicate">The condition a message must match to be processed.</param>
    /// <returns>The same instance, for chaining.</returns>
    public ServiceBusSessionProcessorBuilder WithFilter(
        Func<ServiceBusMetadata, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// Sets the options of the underlying <see cref="ServiceBusSessionProcessor"/>, e.g. the number of
    /// concurrent sessions or specific session ids to process.
    /// </summary>
    /// <param name="options">The options passed to the underlying processor.</param>
    /// <returns>The same instance, for chaining.</returns>
    /// <remarks>
    /// Messages are always settled explicitly, so <see cref="ServiceBusSessionProcessorOptions.AutoCompleteMessages"/> is ignored.
    /// </remarks>
    public ServiceBusSessionProcessorBuilder WithProcessorOptions(
        ServiceBusSessionProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ProcessorOptions = options;
        return this;
    }
}