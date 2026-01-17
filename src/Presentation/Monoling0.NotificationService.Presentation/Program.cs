using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monoling0.NotificationService.Application.Composition;
using Monoling0.NotificationService.Persistence.Extensions;
using Monoling0.NotificationService.Presentation.Email.Extensions;
using Monoling0.NotificationService.Presentation.Kafka.Extensions;
using Monoling0.NotificationService.Serialization;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPersistenceOptions(builder.Configuration);
builder.Services.AddPersistence();

builder.Services.AddSingleton<IJsonSerializer, JsonTextSerializer>();

builder.Services.AddApplication();

builder.Services.AddPresentationEmailOptions(builder.Configuration);
builder.Services.AddPresentationEmail();

builder.Services.AddPresentationKafkaOptions(builder.Configuration);
builder.Services.AddPresentationKafka();

using IHost host = builder.Build();
await host.RunAsync();
