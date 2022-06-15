using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public interface IServiceBusClientFactory
{
    ServiceBusClient Create();
}