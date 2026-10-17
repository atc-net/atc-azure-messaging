var builder = DistributedApplication.CreateBuilder(args);

// Azurite hosts the Storage Queues and the blob container used for Event Hubs checkpoints.
var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator();

var checkpoints = storage.AddBlobs("checkpoints");
var queues = storage.AddQueues("queues");

var eventHubs = builder
    .AddAzureEventHubs("eventhubs")
    .RunAsEmulator();

eventHubs.AddHub("orders");

var serviceBus = builder
    .AddAzureServiceBus("servicebus")
    .RunAsEmulator();

serviceBus.AddServiceBusQueue("shipments");

builder
    .AddProject<Atc_Azure_Messaging_Sample_Worker>("worker")
    .WithReference(eventHubs)
    .WithReference(checkpoints)
    .WithReference(serviceBus)
    .WithReference(queues)
    .WaitFor(eventHubs)
    .WaitFor(serviceBus)
    .WaitFor(storage);

await builder.Build().RunAsync();