namespace Atc.Azure.Messaging.Tests.ServiceBus;

public class ServiceBusClientFactoryTests
{
    [Fact]
    public void Create_Returns_NotNull()
    {
        const string endpoint = "sb://servicebus.windows.net/";
        const string connectionString = $"Endpoint={endpoint};SharedAccessKeyName=<KeyName>;SharedAccessKey=<KeyValue>;";

        new ServiceBusClientFactory(new ServiceBusOptions { ConnectionString = connectionString })
            .Create()
            .Should()
            .NotBeNull();
    }
}