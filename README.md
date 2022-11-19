[![NuGet Version](https://img.shields.io/nuget/v/atc.azure.messaging.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Atc.Azure.Messaging)


# ATC Azure Messaging

A .NET component library for publishing messages on Azure Event Hubs and Service Bus. This targets .NET Standard 2.1 and depends on [ATC Azure Options](https://github.com/atc-net/atc-azure-options).

# Getting Started

The first thing you need to do to make sure that your configuration is setup correctly. For the sake of simplicity, let's use the `appsettings.json` file created with every .NET project

The following is an example of how your would configure your EventHub or ServiceBus for [ATC Azure Options](https://github.com/atc-net/atc-azure-options)

```json
{  
  "EventHubOptions": {
    "ConnectionString": "Endpoint=sb://[your eventhub namespace].servicebus.windows.net/;SharedAccessKeyName=[eventhub name];SharedAccessKey=[sas key]"    
  },
  "ServiceBusOptions": {
    "ConnectionString": "Endpoint=sb://[your servicebus namespace].servicebus.windows.net/;SharedAccessKeyName=[topic|queue name];SharedAccessKey=[sas key]"
  }
}
```

When binding to KeyVault this should of course be `EventHubOptions--ConnectionString` for EventHub or `ServiceBusOptions--ConnectionString` for ServiceBus

The next step is to register the dependencies required to use this component library. For this you can use the `IServiceCollection` extension method `ConfigureMessagingServices(IConfiguration)`. This piggy backs over the `Microsoft.Extensions.DependencyInjection` namespace so that you don't need to include anything extra using statements

Here's an example of how to do this using a Minimal API setup

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Atc.Azure.Messaging dependencies
builder.Services.ConfigureMessagingServices(builder.Configuration);
```

## Publishing to EventHub

To publish events to an EventHub you need an instance of `IEventHubPublisher`, this can be constructed via the `IEventHubPublisherFactory` which exposes the `Create(string eventHubName)` method

```csharp
internal class FooPublisher 
{
    private readonly IEventHubPublisher publisher;

    public FooPublisher(IEventHubPublisherFactory factory)
    {
        publisher = factory.Create("[existing eventhub name]");
    }

    public Task Publish(object message)
        => publisher.PublishAsync(message);
}
```

## Publishing to ServiceBus

To publish events to a ServiceBus topic or queue you need an instance of `IServiceBusPublisher`

```csharp
internal class BarPublisher 
{
    private readonly IServiceBusPublisher publisher;

    public BarPublisher(IServiceBusPublisher publisher)
    {
        this.publisher = publisher;
    }

    public Task Publish(object message)
        => publisher.PublishAsync("[existing servicebus topic]", message);
}
```

Here's a full example of how to use the publishers above using a Minimal API setup (SwaggerUI enabled) with a single endpoint called `POST /data` that accepts a simple request body `{ "a": "string", "b": "string", "c": "string" }` which publishes the request to an EventHub and a ServiceBus topic

```csharp
using Atc.Azure.Messaging.EventHub;
using Atc.Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SendDataHandler>();

// Register Atc.Azure.Messaging dependencies
builder.Services.ConfigureMessagingServices(builder.Configuration);

var app = builder.Build();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost(
    "/data",
    (Request request, SendDataHandler handler) => handler.Post(request));

app.Run();

internal class SendDataHandler
{
    private readonly IEventHubPublisher eventHubPublisher;
    private readonly IServiceBusPublisher serviceBusPublisher;

    public SendDataHandler(
        IEventHubPublisherFactory eventHubFactory,
        IServiceBusPublisher serviceBusPublisher)
    {
        eventHubPublisher = eventHubFactory.Create("[existing eventhub]");
        this.serviceBusPublisher = serviceBusPublisher;
    }

    public async Task<Response> Post(Request request)
    {
        await eventHubPublisher.PublishAsync(request);

        await serviceBusPublisher.PublishAsync("existing topic|queue", request);

        return new Response(
            Guid.NewGuid().ToString("N"),
            request.A,
            request.B,
            request.C);
    }
}

internal record Request(string A, string B, string C);

internal record Response(string Id, string A, string B, string C);
```


# The workflow setup for this repository
[Read more on Git-Flow](https://github.com/atc-net/atc/tree/master/docs/GitFlow.md)

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
