namespace Atc.Azure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(
        object message,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default);
}