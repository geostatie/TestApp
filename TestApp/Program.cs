using System.Text.Json;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";

// Optional on purpose: without a connection string the API still starts and serves
// everything else, it just does not expose the queueing endpoint. A missing value
// should degrade one feature, not crash the app.
var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"];
var reportQueueName = builder.Configuration["ServiceBus:QueueName"] ?? "reports";
var queueingEnabled = !string.IsNullOrWhiteSpace(serviceBusConnection);

if (queueingEnabled)
{
    builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));
    builder.Services.AddSingleton(sp =>
        sp.GetRequiredService<ServiceBusClient>().CreateSender(reportQueueName));
}

// Only Swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allowed origins come from config, never hardcoded. In Azure they arrive as
// the env var Cors__AllowedOrigins__0. An empty list allows nothing.
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        // Browsers hide non-standard response headers from JavaScript unless
        // they are explicitly exposed, so the SPA could not read X-Served-By.
        .WithExposedHeaders("X-Served-By"));
});

var app = builder.Build();

// Identifies the replica handling the request. Container Apps injects
// CONTAINER_APP_REPLICA_NAME; the machine name is the fallback for local runs.
// Resolved once at startup because it cannot change for the life of the process.
var replicaName = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME")
                  ?? Environment.MachineName;

// Stamp every response so load balancing across replicas is observable from
// the outside. First in the pipeline, so it covers Swagger and errors too.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Served-By"] = replicaName;
    await next();
});

// Swagger JSON + UI
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Must sit after routing and before the endpoints, or the CORS headers are
// silently dropped from responses.
app.UseCors(FrontendCorsPolicy);

app.MapGet("/weatherforecast", () =>
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();

        return forecast;
    })
    .WithName("GetWeatherForecast");

if (queueingEnabled)
{
    // Accepts work and returns immediately. Nothing slow is ever attached to an
    // HTTP request, so the replica can be killed at any moment without losing it.
    app.MapPost("/reports", async (QueueReportRequest request, ServiceBusSender sender, CancellationToken ct) =>
        {
            var jobId = Guid.NewGuid().ToString("n");

            var payload = JsonSerializer.Serialize(new
            {
                jobId,
                subject = request.Subject,
                workSeconds = Math.Clamp(request.WorkSeconds, 1, 60),
            });

            // MessageId is the job id, which also gives Service Bus a natural
            // key if duplicate detection is ever enabled.
            await sender.SendMessageAsync(new ServiceBusMessage(payload) { MessageId = jobId }, ct);

            // 202, not 200: the work is queued, not done.
            return Results.Accepted($"/reports/{jobId}", new { jobId, status = "queued" });
        })
        .WithName("QueueReport");
}

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <param name="Subject">Use "fail" to exercise the retry and dead-letter path.</param>
/// <param name="WorkSeconds">How long the worker should pretend to work, 1-60.</param>
internal record QueueReportRequest(string Subject, int WorkSeconds = 5);