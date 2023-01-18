namespace Atc.Azure.Messaging.ServiceBus;

internal sealed class ServiceBusClientFactory : IServiceBusClientFactory
{
    private readonly ServiceBusOptions options;

    public ServiceBusClientFactory(ServiceBusOptions options)
    {
        this.options = options;
    }

    public ServiceBusClient Create() => new ServiceBusClient(options.ConnectionString);
}