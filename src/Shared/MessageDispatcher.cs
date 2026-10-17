namespace Atc.Azure.Messaging.Internal;

/// <summary>
/// Deserializes received payloads and hands them to the registered message processor,
/// resolving the processor from a new service scope for every message.
/// </summary>
/// <remarks>
/// Shared by all transports; compiled into each transport assembly as an internal type.
/// </remarks>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <typeparam name="TProcessor">The type of processor messages are handed to.</typeparam>
/// <typeparam name="TMetadata">The transport specific metadata type.</typeparam>
internal sealed partial class MessageDispatcher<TMessage, TProcessor, TMetadata>
    where TProcessor : class, IMessageProcessor<TMessage>
    where TMetadata : MessageMetadata
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMessageSerializer serializer;
    private readonly IReadOnlyList<Func<TMetadata, bool>> filters;
    private readonly ILogger logger;

    public MessageDispatcher(
        IServiceScopeFactory scopeFactory,
        IMessageSerializer serializer,
        IReadOnlyList<Func<TMetadata, bool>> filters,
        ILogger logger)
    {
        this.scopeFactory = scopeFactory;
        this.serializer = serializer;
        this.filters = filters;
        this.logger = logger;
    }

    /// <summary>
    /// Dispatches a received message to the processor.
    /// </summary>
    /// <param name="payload">The received payload.</param>
    /// <param name="metadata">Metadata about the message, or <see langword="null"/> when the error is not related to a specific message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    /// <returns>The outcome of the dispatch, which the transport uses to settle the message.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is canceled while the processor is running.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Processor failures are reported to the error handler instead of crashing the transport.")]
    public async Task<MessageDispatchResult> DispatchAsync(
        BinaryData payload,
        TMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (!PassesFilters(metadata))
        {
            return MessageDispatchResult.Filtered;
        }

        TMessage? message;
        try
        {
            message = serializer.Deserialize<TMessage>(payload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ReportErrorAsync(
                    new MessageDeserializationException(typeof(TMessage), metadata.MessageId, ex),
                    metadata,
                    processor: null,
                    cancellationToken)
                .ConfigureAwait(false);

            return MessageDispatchResult.DeserializationFailed;
        }

        if (message is null)
        {
            await ReportErrorAsync(
                    new MessageDeserializationException(typeof(TMessage), metadata.MessageId, innerException: null),
                    metadata,
                    processor: null,
                    cancellationToken)
                .ConfigureAwait(false);

            return MessageDispatchResult.DeserializationFailed;
        }

        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();
            try
            {
                await processor
                    .ProcessAsync(message, metadata, cancellationToken)
                    .ConfigureAwait(false);

                return MessageDispatchResult.Processed;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await ReportErrorAsync(ex, metadata, processor, cancellationToken)
                    .ConfigureAwait(false);

                return MessageDispatchResult.ProcessingFailed;
            }
        }
    }

    /// <summary>
    /// Reports an error to the processor, when it implements <see cref="IProcessErrorHandler"/>, or logs it.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="metadata">Metadata about the message, or <see langword="null"/> when the error is not related to a specific message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used.</param>
    public async Task ReportErrorAsync(
        Exception exception,
        MessageMetadata? metadata,
        CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();
            await ReportErrorAsync(exception, metadata, processor, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private bool PassesFilters(TMetadata metadata)
    {
        foreach (var filter in filters)
        {
            if (!filter(metadata))
            {
                return false;
            }
        }

        return true;
    }

    private async Task ReportErrorAsync(
        Exception exception,
        MessageMetadata? metadata,
        TProcessor? processor,
        CancellationToken cancellationToken)
    {
        if (processor is null)
        {
            await ReportErrorAsync(exception, metadata, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (processor is not IProcessErrorHandler errorHandler)
        {
            LogProcessingFailed(
                exception,
                typeof(TMessage).Name,
                typeof(TProcessor).Name,
                metadata?.MessageId);
            return;
        }

        try
        {
            await errorHandler
                .ProcessErrorAsync(exception, metadata, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogErrorHandlerFailed(
                ex,
                typeof(TProcessor).Name,
                metadata?.MessageId);
        }
    }

    [LoggerMessage(
        EventId = 1_000,
        Level = LogLevel.Warning,
        Message = "Failed to process {MessageType} message {MessageId} in {ProcessorType}")]
    private partial void LogProcessingFailed(
        Exception exception,
        string messageType,
        string processorType,
        string? messageId);

    [LoggerMessage(
        EventId = 1_010,
        Level = LogLevel.Error,
        Message = "The error handler of {ProcessorType} failed while handling an error for message {MessageId}")]
    private partial void LogErrorHandlerFailed(
        Exception exception,
        string processorType,
        string? messageId);
}