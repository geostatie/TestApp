// Runs the whole system locally with one command: dotnet run --project TestApp.AppHost
//
// The apps do not know Aspire exists. They read the same configuration keys they
// read in Azure (ServiceBus:ConnectionString, ServiceBus:QueueName), and this file
// only supplies local values for them.
var builder = DistributedApplication.CreateBuilder(args);

// The Service Bus emulator, in Docker. Aspire pulls the image, starts it with its
// SQL dependency, accepts the EULA and creates the queue declared below.
var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

// Mirrors the queue in Azure: 5 deliveries, then the message is dead-lettered.
// ReportWorker relies on this for the "fail" demonstration path.
serviceBus.AddServiceBusQueue("reports")
    .WithProperties(queue => queue.MaxDeliveryCount = 5);

// The "http" profile keeps the API on http://localhost:5280, which is what
// testapp-frontend/public/config.json already points at.
var api = builder.AddProject<Projects.TestApp>("api", launchProfileName: "http")
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WaitFor(serviceBus);

// The worker throws on startup without a connection string, so it must not start
// before the emulator is healthy.
builder.AddProject<Projects.TestApp_Worker>("worker")
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WaitFor(serviceBus);

// ng serve always listens on 4200, the origin the API's CORS policy allows in
// Development. Not proxied, so Aspire does not try to assign a different port.
builder.AddJavaScriptApp("frontend", "../testapp-frontend", "start")
    .WithHttpEndpoint(port: 4200, isProxied: false)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
