global using System.Collections.Concurrent;
global using System.Diagnostics.CodeAnalysis;
global using System.Text;
global using System.Text.Json;

global using Atc.Azure.Messaging.EventHub;
global using Atc.Azure.Messaging.ServiceBus;
global using Atc.Azure.Options.Authorization;
global using Atc.Azure.Options.Environment;
global using Atc.Azure.Options.EventHub;
global using Atc.Azure.Options.Providers;
global using Atc.Azure.Options.ServiceBus;

global using Azure.Identity;
global using Azure.Messaging.EventHubs;
global using Azure.Messaging.EventHubs.Producer;
global using Azure.Messaging.ServiceBus;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection.Extensions;