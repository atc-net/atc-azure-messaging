using Atc.Azure.Options.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace Atc.Azure.Messaging.ServiceBus;

public class ServiceBusClientFactory : IServiceBusClientFactory
{
    private readonly ServiceBusOptions options;

    public ServiceBusClientFactory(ServiceBusOptions options)
    {
        this.options = options;
    }

    public ServiceBusClient Create() => new(options.ConnectionString);
}