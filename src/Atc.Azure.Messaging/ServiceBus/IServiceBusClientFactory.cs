using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

internal interface IServiceBusClientFactory
{
    ServiceBusClient Create();
}