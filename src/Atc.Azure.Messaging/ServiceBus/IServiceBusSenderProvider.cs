using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public interface IServiceBusSenderProvider
{
    ServiceBusSender GetSender(string topicName);
}