#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable MA0047 // Declare types in namespaces
#pragma warning disable S3903 // Types should be defined in named namespaces

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

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal class SendDataHandler
{
    private readonly IEventHubPublisher eventHubPublisher;
    private readonly IServiceBusPublisher serviceBusPublisher;

    public SendDataHandler(
        IEventHubPublisherFactory eventHubFactory,
        IServiceBusPublisher serviceBusPublisher)
    {
        eventHubPublisher = eventHubFactory.Create("[existing eventhub");
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

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal record Request(string A, string B, string C);

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal record Response(string Id, string A, string B, string C);

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static partial class Program { }