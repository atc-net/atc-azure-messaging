# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

```bash
# Build everything (warnings are errors in Release)
dotnet build Atc.Azure.Messaging.slnx -c Release

# Run all unit tests, as CI does
dotnet test -c Release --filter-query "/[category!=integration]"

# Run the tests of one project
dotnet test --project test/Atc.Azure.Messaging.ServiceBus.Tests

# Run the Aspire sample (needs Docker for the emulators)
dotnet run --project sample/Atc.Azure.Messaging.Sample.AppHost
```

Tests use xunit.v3 on Microsoft Testing Platform (see `global.json`), with Atc.Test, AwesomeAssertions and NSubstitute.

## Architecture Overview

.NET 10 library with typed publishers and hosted processors for Azure Event Hubs, Service Bus and Storage Queues, behind transport agnostic abstractions. All four packages share one version and are released together.

### Projects

- `src/Atc.Azure.Messaging.Abstractions/` - `IMessagePublisher<T>`, `IMessageProcessor<T>`, `IProcessErrorHandler`, `IMessageProcessorService<T>`, `MessageMetadata`, `PublishingOptions`, `IMessageSerializer` / `JsonMessageSerializer`. Root namespace `Atc.Azure.Messaging`.
- `src/Atc.Azure.Messaging.EventHub/` - Event Hubs publishers, checkpointed processor (`PluggableCheckpointStoreEventProcessor` + `BlobCheckpointStore`) and stateless processor (`EventHubConsumerClient`).
- `src/Atc.Azure.Messaging.ServiceBus/` - Service Bus publishers (incl. scheduling), queue, subscription and session processors.
- `src/Atc.Azure.Messaging.StorageQueue/` - Storage Queue publishers and a polling `BackgroundService` processor with a poison queue.
- `src/Shared/` - internal types compiled into every transport via `<Compile Include="..\Shared\*.cs" Link=... />`: `MessageDispatcher` (filters, deserialization, per-message DI scope, error reporting), `MessageDispatchResult`, `PropertyNameResolver`.
- `test/Common/` - test helpers and `MessageDispatcherTests`, linked into each transport test project the same way.
- `sample/` - Aspire AppHost plus a worker chaining Event Hubs → Service Bus → Storage Queue.

### Shape of a transport

Each transport follows the same layout:

- `Extensions/ServiceCollectionExtensions` - `Add{Transport}Messaging(configure)` and `Add{Transport}Messaging(connectionName, configure)`, in the `Microsoft.Extensions.DependencyInjection` namespace.
- `Builders/` - `{Transport}MessagingBuilder` (`Configure`, `AddPublisher`, `AddProcessor`), plus publisher and processor builders holding per-registration defaults.
- `Options/{Transport}MessagingOptions` - named options per connection (credential or connection string, serializer, client options).
- `Providers/Internal/` - client factory validating options and caching clients and senders per connection.
- `Publishing/Internal/` - the publisher, the publisher factory and a registration record per `AddPublisher` call.
- `Processing/Internal/` - processor services mapping `MessageDispatchResult` to settlement.

### Key Implementation Patterns

- **Named connections**: options are named options keyed by connection name (`null` is the default connection). Publishers are keyed singletons under the connection name, with an unkeyed `TryAdd` fallback so the default connection wins.
- **Processor registrations** use a unique `object` key per call, so one processor type can be registered several times. The processor is `TryAddSingleton`'d and resolved from a new scope per message.
- **Settlement**: Service Bus completes processed and filtered messages, dead-letters unreadable ones and abandons failures (`AutoCompleteMessages` is forced off). Storage Queue deletes processed messages, leaves failures for retry and moves them to the poison queue at `MaxDequeueCount` or on deserialization failure. Event Hubs checkpoints after each batch, but not when cancelled.
- **Cancellation**: `OperationCanceledException` is only treated as shutdown when the processing token is cancelled; otherwise it is a processing failure.
- **Testability**: Azure SDK clients are mocked with NSubstitute and built with `EventHubsModelFactory`, `ServiceBusModelFactory` and `QueuesModelFactory`. Internals are visible to `$(MSBuildProjectName).Tests`. Retry delays are `internal init` properties and processors take a `TimeProvider`.

## Conventions

- Analyzers (Atc.Analyzer, StyleCop, Meziantou, Sonar, AsyncFixer, CA) run with `AnalysisMode=AllEnabledByDefault`; Release builds treat warnings as errors.
- Files do not end with a newline (SA1518).
- ATC201: a single parameter stays on the declaration line only when the line fits in 80 characters; otherwise it goes on its own line.
- One type per file, matching the file name. Public members with a `<summary>` also need `<param>`, `<typeparam>` and `<returns>` docs.
- Tests: sealed classes, `Method_Should_Behavior` names and `// Arrange`, `// Act`, `// Assert` sections.

## Releases

- release-please (`release-please-config.json`, `.release-please-manifest.json`) bumps `<Version>` in the root `Directory.Build.props` from Conventional Commits on `main`.
- `.github/workflows/release-please.yml` packs the solution and pushes to NuGet with Trusted Publishing (`NuGet/login`, OIDC).
- Commit messages are Conventional Commits (`feat(servicebus): ...`, `fix(eventhub): ...`), without `Co-Authored-By` or session trailers.