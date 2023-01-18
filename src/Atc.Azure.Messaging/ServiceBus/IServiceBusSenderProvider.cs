using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

internal interface IServiceBusSenderProvider
{
    ServiceBusSender GetSender(string topicName);
}