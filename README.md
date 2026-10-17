# Atc.Azure.Messaging

Typed publishers and hosted processors for Azure Event Hubs, Azure Service Bus and Azure Storage Queues, built on a small set of transport agnostic abstractions.

| Package | Description | NuGet |
|---|---|---|
| `Atc.Azure.Messaging.Abstractions` | `IMessagePublisher<T>`, `IMessageProcessor<T>`, metadata and serialization | [![NuGet Version](https://img.shields.io/nuget/v/atc.azure.messaging.abstractions.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Atc.Azure.Messaging.Abstractions) |
| `Atc.Azure.Messaging.EventHub` | Event Hubs publishers, checkpointed and stateless processors | [![NuGet Version](https://img.shields.io/nuget/v/atc.azure.messaging.eventhub.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Atc.Azure.Messaging.EventHub) |
| `Atc.Azure.Messaging.ServiceBus` | Service Bus publishers, queue, subscription and session processors | [![NuGet Version](https://img.shields.io/nuget/v/atc.azure.messaging.servicebus.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Atc.Azure.Messaging.ServiceBus) |
| `Atc.Azure.Messaging.StorageQueue` | Storage Queue publishers and polling processors with a poison queue | [![NuGet Version](https://img.shields.io/nuget/v/atc.azure.messaging.storagequeue.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Atc.Azure.Messaging.StorageQueue) |

All packages share one version and are released together.

## Table of Content

- [Atc.Azure.Messaging](#atcazuremessaging)
  - [Table of Content](#table-of-content)
  - [Features](#features)
  - [Getting started](#getting-started)
    - [Publishing](#publishing)
    - [Processing](#processing)
  - [Abstractions](#abstractions)
    - [Publishers](#publishers)
    - [Processors](#processors)
    - [Error handling](#error-handling)
    - [Serialization](#serialization)
  - [Connections](#connections)
    - [Credentials or connection strings](#credentials-or-connection-strings)
    - [Configuration binding](#configuration-binding)
    - [Named connections](#named-connections)
  - [Event Hubs](#event-hubs)
    - [Event Hub publishers](#event-hub-publishers)
    - [Checkpointed processors](#checkpointed-processors)
    - [Stateless processors](#stateless-processors)
  - [Service Bus](#service-bus)
    - [Service Bus publishers](#service-bus-publishers)
    - [Scheduled messages](#scheduled-messages)
    - [Queue and subscription processors](#queue-and-subscription-processors)
    - [Session processors](#session-processors)
    - [Message settlement](#message-settlement)
  - [Storage Queues](#storage-queues)
    - [Storage Queue publishers](#storage-queue-publishers)
    - [Storage Queue processors](#storage-queue-processors)
  - [Sample](#sample)
  - [Migrating from 1.x](#migrating-from-1x)
  - [Requirements](#requirements)
  - [How to contribute](#how-to-contribute)

## Features

- **One programming model for three transports**: publish with `IMessagePublisher<T>` and handle messages with `IMessageProcessor<T>`, whether they travel over Event Hubs, Service Bus or Storage Queues.
- **Typed publishers** with per type defaults for message id, correlation id, partition key, session id, subject, time to live and application properties.
- **Batch publishing** that groups messages by partition key or session and splits them into as many batches as needed.
- **Hosted processors** registered as `IHostedService`, with the processor resolved from a new dependency injection scope for every message.
- **Reliable settlement**: Service Bus messages are completed, abandoned or dead-lettered depending on the outcome, and Storage Queue messages are moved to a poison queue after the last attempt.
- **Event Hubs checkpointing** in Blob Storage with load balancing across instances, plus a stateless processor for fan-out scenarios.
- **Service Bus extras**: sessions, topics and subscriptions, and scheduled messages that can be cancelled.
- **Named connections** registered as keyed services, so one application can talk to several namespaces or storage accounts.
- **Entra ID first**: connect with a `TokenCredential` such as `DefaultAzureCredential`, or with a connection string.
- **Pluggable serialization**, with `System.Text.Json` by default.

## Getting started

Install the package for the transport you use. It brings in the abstractions.

```bash
dotnet add package Atc.Azure.Messaging.ServiceBus
```

### Publishing

Register a publisher for a message type, and the queue or topic it is sent to:

```csharp
builder.Services.AddServiceBusMessaging(sb => sb
    .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    .AddPublisher<OrderPlaced>("orders", p => p
        .WithMessageId(m => m.OrderId.ToString())
        .WithSubject("order-placed")));
```

Inject `IMessagePublisher<T>` and publish:

```csharp
public sealed class CheckoutService(IMessagePublisher<OrderPlaced> publisher)
{
    public Task PlaceOrderAsync(Order order, CancellationToken cancellationToken)
        => publisher.PublishAsync(new OrderPlaced(order.Id, order.CustomerId), cancellationToken);
}
```

### Processing

Implement `IMessageProcessor<T>`:

```csharp
public sealed class OrderPlacedProcessor(ILogger<OrderPlacedProcessor> logger) : IMessageProcessor<OrderPlaced>
{
    public Task ProcessAsync(OrderPlaced message, MessageMetadata metadata, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was placed", message.OrderId);
        return Task.CompletedTask;
    }
}
```

And register it for the queue or subscription to read:

```csharp
builder.Services.AddServiceBusMessaging(sb => sb
    .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    .AddProcessor<OrderPlaced, OrderPlacedProcessor>("orders"));
```

The processor starts and stops with the host.

## Abstractions

`Atc.Azure.Messaging.Abstractions` holds the types your application code depends on. Code that publishes or processes messages only needs this package, so it can be tested without Azure and moved to another transport by changing the registration alone.

### Publishers

```csharp
public interface IMessagePublisher<in TMessage>
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    Task PublishAsync(TMessage message, PublishingOptions options, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IReadOnlyCollection<TMessage> messages, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IReadOnlyCollection<TMessage> messages, PublishingOptions options, CancellationToken cancellationToken = default);
}
```

`PublishingOptions` overrides the publisher defaults for a single call: `MessageId`, `CorrelationId`, `ContentType`, `PartitionKey` and `Properties`. Each transport has a subclass with its own settings, e.g. `ServiceBusPublishingOptions.SessionId`.

Each transport also registers a richer interface, `IEventHubPublisher<T>`, `IServiceBusPublisher<T>` or `IStorageQueuePublisher<T>`, and a factory (`IEventHubPublisherFactory` etc.) for resolving publishers by connection name.

### Processors

```csharp
public interface IMessageProcessor<in TMessage>
{
    Task ProcessAsync(TMessage message, MessageMetadata metadata, CancellationToken cancellationToken);
}
```

`MessageMetadata` carries `MessageId`, `CorrelationId`, `ContentType`, `PartitionKey`, `EnqueuedTime` and the application `Properties`. Cast it to `EventHubMetadata`, `ServiceBusMetadata` or `StorageQueueMetadata` for transport details such as the partition, the delivery count or the dequeue count.

The processor type is registered as a singleton unless you register it yourself first. It is resolved from a new scope for every message, so register it as scoped to get scoped dependencies such as a `DbContext` per message:

```csharp
builder.Services.AddScoped<OrderPlacedProcessor>();
```

Every registered processor is also available as `IMessageProcessorService<TProcessor>`, which exposes `IsRunning` and lets you start and stop it yourself.

### Error handling

When processing throws, the error is logged and the message is retried as the transport allows. Implement `IProcessErrorHandler` on the processor to handle errors yourself:

```csharp
public sealed class OrderPlacedProcessor : IMessageProcessor<OrderPlaced>, IProcessErrorHandler
{
    public Task ProcessAsync(OrderPlaced message, MessageMetadata metadata, CancellationToken cancellationToken) => ...;

    public Task ProcessErrorAsync(Exception exception, MessageMetadata? metadata, CancellationToken cancellationToken)
    {
        // metadata is null for errors that are not about a specific message, e.g. a lost connection.
        return Task.CompletedTask;
    }
}
```

A payload that cannot be deserialized is reported as a `MessageDeserializationException` and is never handed to `ProcessAsync`.

### Serialization

Messages are serialized as JSON with `System.Text.Json` and the web defaults (camelCase). Pass your own options, or replace the serializer with an `IMessageSerializer` implementation:

```csharp
sb.Configure(o => o
    .WithConnectionString(connectionString)
    .WithJsonSerializerOptions(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    }));
```

## Connections

### Credentials or connection strings

Every transport accepts a `TokenCredential` or a connection string:

```csharp
eh.Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()));
sb.Configure(o => o.WithConnectionString("Endpoint=sb://my-namespace.servicebus.windows.net/;..."));
sq.Configure(o => o.WithConnection(new Uri("https://myaccount.queue.core.windows.net"), new DefaultAzureCredential()));
```

The Azure SDK client options can be set too, with `WithClientOptions(...)`, or `WithProducerClientOptions(...)` and `WithConsumerClientOptions(...)` for Event Hubs.

### Configuration binding

The options can be bound to a configuration section:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://my-namespace.servicebus.windows.net/;..."
  }
}
```

```csharp
builder.Services.AddServiceBusMessaging(sb => sb
    .Configure(builder.Configuration.GetSection("ServiceBus"))
    .AddPublisher<OrderPlaced>("orders"));
```

`Configure<TConfigureOptions>()` registers an `IConfigureNamedOptions<T>` from dependency injection, for options that depend on other services.

### Named connections

Pass a connection name to talk to more than one namespace or storage account:

```csharp
builder.Services
    .AddServiceBusMessaging("orders", sb => sb
        .Configure(o => o.WithConnection("orders-namespace.servicebus.windows.net", credential))
        .AddPublisher<OrderPlaced>("orders"))
    .AddServiceBusMessaging("billing", sb => sb
        .Configure(o => o.WithConnection("billing-namespace.servicebus.windows.net", credential))
        .AddPublisher<InvoiceCreated>("invoices"));
```

Publishers on a named connection are registered as keyed services under the connection name:

```csharp
public sealed class BillingService([FromKeyedServices("billing")] IMessagePublisher<InvoiceCreated> publisher)
```

They are also registered without a key, unless a publisher for the same message type exists on the default connection, which then wins. The publisher factories resolve publishers by name too: `factory.Create<InvoiceCreated>("billing")`.

## Event Hubs

```bash
dotnet add package Atc.Azure.Messaging.EventHub
```

### Event Hub publishers

```csharp
builder.Services.AddEventHubMessaging(eh => eh
    .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    .AddPublisher<TelemetryReceived>("telemetry", p => p
        .WithPartitionKey(m => m.DeviceId)
        .WithMessageId(m => m.ReadingId)
        .WithProperty(m => m.TenantId)
        .WithProperty("schema", "v2")));
```

Events with the same partition key end up on the same partition, in order. `WithProperty(m => m.TenantId)` adds an application property named after the member. `EventHubPublishingOptions.PartitionId` sends to a specific partition instead.

`PublishBatchAsync` groups events by partition key and sends them in as few batches as the size limit allows.

### Checkpointed processors

`AddProcessor` reads an Event Hub with load balancing across all running instances, and stores checkpoints and partition ownership in Blob Storage:

```csharp
builder.Services.AddEventHubMessaging(eh => eh
    .Configure(o => o
        .WithConnection("my-namespace.servicebus.windows.net", credential)
        .WithCheckpointStorage(new Uri("https://myaccount.blob.core.windows.net"), credential))
    .AddProcessor<TelemetryReceived, TelemetryProcessor>("telemetry", consumerGroup: "processing", p => p
        .WithCheckpointContainer("telemetry-checkpoints")
        .WithMaxBatchSize(50)
        .WithFilter(m => m.Properties.ContainsKey("schema"))));
```

- The checkpoint container defaults to the Event Hub name, and is created when processing starts unless you pass `createIfNotExists: false`.
- A checkpoint is written after each batch. Filtered events and events that failed are checkpointed too, since Event Hubs has no redelivery. Failures are reported to the error handler.
- When processing stops or a partition moves to another instance, no checkpoint is written for the unfinished batch, so the next owner picks it up again.
- `WithProcessorOptions(...)` exposes the `EventProcessorOptions`, e.g. the load balancing strategy.

### Stateless processors

`AddStatelessProcessor` reads every partition without checkpointing. Every running instance receives every event, which suits cache invalidation, live dashboards and similar fan-out scenarios:

```csharp
eh.AddStatelessProcessor<PriceChanged, PriceCacheInvalidator>("prices", configure: p => p
    .WithStartingPosition(EventPosition.Latest));
```

Reading resumes after the last event when a transient error occurs.

## Service Bus

```bash
dotnet add package Atc.Azure.Messaging.ServiceBus
```

### Service Bus publishers

```csharp
builder.Services.AddServiceBusMessaging(sb => sb
    .Configure(o => o.WithConnection("my-namespace.servicebus.windows.net", new DefaultAzureCredential()))
    .AddPublisher<ShipOrder>("shipments", p => p
        .WithMessageId(m => m.OrderId.ToString())
        .WithSessionId(m => m.CustomerId)
        .WithSubject("ship-order")
        .WithTimeToLive(TimeSpan.FromDays(1))
        .WithProperty(m => m.Warehouse)));
```

`ServiceBusPublishingOptions` adds `SessionId`, `TimeToLive`, `ScheduledEnqueueTime`, `Subject`, `ReplyTo`, `ReplyToSessionId` and `To` per call. `PublishBatchAsync` groups messages by session id and partition key.

### Scheduled messages

```csharp
var sequenceNumber = await publisher.SchedulePublishAsync(reminder, DateTimeOffset.UtcNow.AddHours(1));

// Changed your mind?
await publisher.CancelScheduledPublishAsync(sequenceNumber);
```

### Queue and subscription processors

```csharp
sb.AddProcessor<ShipOrder, ShipOrderProcessor>("shipments", p => p
    .WithProcessorOptions(new ServiceBusProcessorOptions { MaxConcurrentCalls = 8 }));

sb.AddProcessor<OrderPlaced, AuditProcessor>("orders", "audit", p => p
    .WithFilter(m => m.Subject == "order-placed"));
```

### Session processors

Session processors handle the messages of one session at a time, in order:

```csharp
sb.AddSessionProcessor<ShipOrder, ShipOrderProcessor>("shipments-by-customer");
sb.AddSessionProcessor<OrderPlaced, OrderSagaProcessor>("orders", "saga");
```

### Message settlement

Messages are settled explicitly, so `AutoCompleteMessages` is always turned off:

| Outcome | Settlement |
|---|---|
| Processed | Completed |
| Rejected by a filter | Completed |
| Payload could not be deserialized | Dead-lettered with the reason `DeserializationFailed` |
| Processing threw | Abandoned, so Service Bus redelivers it and dead-letters it after `MaxDeliveryCount` attempts |

## Storage Queues

```bash
dotnet add package Atc.Azure.Messaging.StorageQueue
```

### Storage Queue publishers

```csharp
builder.Services.AddStorageQueueMessaging(sq => sq
    .Configure(o => o.WithConnection(new Uri("https://myaccount.queue.core.windows.net"), new DefaultAzureCredential()))
    .AddPublisher<SendReceipt>("receipts", p => p
        .CreateIfNotExists()
        .WithTimeToLive(TimeSpan.FromDays(7))));
```

`StorageQueuePublishingOptions` sets an `InitialVisibilityDelay` and `TimeToLive` per call. Storage Queues have no message properties, so the other `PublishingOptions` are ignored.

### Storage Queue processors

```csharp
sq.AddProcessor<SendReceipt, SendReceiptProcessor>("receipts", p => p
    .WithPollingInterval(TimeSpan.FromSeconds(2))
    .WithMaxMessages(32)
    .WithVisibilityTimeout(TimeSpan.FromMinutes(1))
    .WithMaxDequeueCount(5)
    .WithPoisonQueue("receipts-failed")
    .CreateIfNotExists());
```

- The queue is polled for up to `MaxMessages` messages (1 to 32, default 16). When it is empty, the processor waits for the polling interval (default 5 seconds).
- Processed messages are deleted.
- When processing throws, the message stays on the queue and is received again once the visibility timeout (default 30 seconds) expires.
- After `MaxDequeueCount` attempts (default 5), or straight away when the payload cannot be read, the message is moved to the poison queue, which defaults to `{queue}-poison`.

## Sample

The [sample](sample) folder has an Aspire AppHost that runs the Event Hubs, Service Bus and Azurite emulators, and a worker that chains all three transports:

1. `OrderProducer` publishes an `OrderPlaced` event to Event Hubs every 5 seconds.
2. `OrderPlacedProcessor` reads it and sends a `ShipOrder` command to a Service Bus queue.
3. `ShipOrderProcessor` handles the command and puts a `SendReceipt` job on a Storage Queue.
4. `SendReceiptProcessor` handles the job.

Run it with Docker running:

```bash
dotnet run --project sample/Atc.Azure.Messaging.Sample.AppHost
```

## Migrating from 1.x

Version 2 is a rewrite with a typed API. The `Atc.Azure.Messaging` package, `ConfigureMessagingServices`, `IMessagePayloadSerializer` and the dependency on `Atc.Azure.Options` are gone.

1. Replace the `Atc.Azure.Messaging` package with the package for each transport you use.
2. Replace `ConfigureMessagingServices(configuration)` with `AddEventHubMessaging(...)` and `AddServiceBusMessaging(...)`, and register a publisher per message type with the Event Hub, topic or queue it is sent to.
3. Replace the untyped `IEventHubPublisherFactory.Create(eventHubName)` and `IServiceBusPublisher.PublishAsync(topicOrQueue, message, ...)` calls with an injected `IMessagePublisher<T>`, `IEventHubPublisher<T>` or `IServiceBusPublisher<T>`.
4. Prefer `WithConnection(namespace, credential)` over connection strings.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Docker, to run the sample

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
