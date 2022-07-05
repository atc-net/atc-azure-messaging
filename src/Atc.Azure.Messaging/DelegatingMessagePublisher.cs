namespace Atc.Azure.Messaging;

public class DelegatingMessagePublisher : IMessagePublisher
{
    private readonly IMessagePublisher[] publishers;

    public DelegatingMessagePublisher(params IMessagePublisher[] publishers)
    {
        this.publishers = publishers;
    }

    public Task PublishAsync(
        object message,
        IDictionary<string, string>? properties = null,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
