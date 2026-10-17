var builder = Host.CreateApplicationBuilder(args);

// The connection strings are provided by the Aspire AppHost, see Atc.Azure.Messaging.Sample.AppHost.
string GetConnectionString(string name)
    => builder.Configuration.GetConnectionString(name)
       ?? throw new InvalidOperationException($"The connection string '{name}' is missing. Run the sample through the AppHost.");

builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddEventHubMessaging(eventHubs => eventHubs
        .Configure(o => o
            .WithConnectionString(GetConnectionString("eventhubs"))
            .WithCheckpointStorage(GetConnectionString("checkpoints")))
        .AddPublisher<OrderPlaced>("orders", p => p
            .WithPartitionKey(m => m.CustomerId)
            .WithMessageId(m => m.OrderId.ToString())
            .WithProperty(m => m.CustomerId))
        .AddProcessor<OrderPlaced, OrderPlacedProcessor>("orders", configure: p => p
            .WithCheckpointContainer("orders-checkpoints")));

builder.Services
    .AddServiceBusMessaging(serviceBus => serviceBus
        .Configure(o => o.WithConnectionString(GetConnectionString("servicebus")))
        .AddPublisher<ShipOrder>("shipments", p => p
            .WithMessageId(m => m.OrderId.ToString())
            .WithSubject("ship-order"))
        .AddProcessor<ShipOrder, ShipOrderProcessor>("shipments"));

builder.Services
    .AddStorageQueueMessaging(storageQueues => storageQueues
        .Configure(o => o.WithConnectionString(GetConnectionString("queues")))
        .AddPublisher<SendReceipt>("receipts", p => p.CreateIfNotExists())
        .AddProcessor<SendReceipt, SendReceiptProcessor>("receipts", p => p
            .CreateIfNotExists()
            .WithPollingInterval(TimeSpan.FromSeconds(1))));

builder.Services.AddHostedService<OrderProducer>();

await builder.Build().RunAsync();