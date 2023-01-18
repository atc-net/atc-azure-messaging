using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

internal sealed class ServiceBusSenderProvider : IServiceBusSenderProvider
{
    private readonly ServiceBusClient client;
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders;

    public ServiceBusSenderProvider(IServiceBusClientFactory factory)
    {
        client = factory.Create();
        senders = new ConcurrentDictionary<string, ServiceBusSender>(StringComparer.OrdinalIgnoreCase);
    }

    public ServiceBusSender GetSender(string topicName)
        => senders.GetOrAdd(
            topicName,
            topic => client.CreateSender(topic));
}