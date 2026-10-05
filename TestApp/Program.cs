var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";

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

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}