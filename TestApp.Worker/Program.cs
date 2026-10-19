using Azure.Messaging.ServiceBus;
using TestApp.Worker;

// Host, not WebApplication: this is a console app. No Kestrel, no ports, no ingress.
var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration["ServiceBus:ConnectionString"]
    ?? throw new InvalidOperationException(
        "ServiceBus:ConnectionString is not configured. In Azure it arrives as the " +
        "env var ServiceBus__ConnectionString, sourced from a container app secret.");

builder.Services.AddSingleton(new ServiceBusClient(connectionString));
builder.Services.AddHostedService<ReportWorker>();

builder.Build().Run();
