using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace TestApp.Worker;

public sealed class ReportWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ServiceBusClient _client;
    private readonly ILogger<ReportWorker> _logger;
    private readonly string _queueName;
    private readonly string _replicaName;

    private ServiceBusProcessor? _processor;

    public ReportWorker(ServiceBusClient client, IConfiguration config, ILogger<ReportWorker> logger)
    {
        _client = client;
        _logger = logger;
        _queueName = config["ServiceBus:QueueName"] ?? "reports";

        // Container Apps injects the replica name; the machine name covers local runs.
        _replicaName = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME")
                       ?? Environment.MachineName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
        {
            // Complete messages explicitly, so a crash mid-work leaves the message
            // on the queue to be redelivered rather than silently acknowledging it.
            AutoCompleteMessages = false,

            // One at a time keeps the logs readable while learning. Raise it to
            // process a backlog faster within a single replica.
            MaxConcurrentCalls = 1,

            // The queue's lock is 1 minute. If the work runs longer, the SDK renews
            // the lock in the background for up to this long.
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
        });

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        _logger.LogInformation("Worker {Replica} listening on queue {Queue}", _replicaName, _queueName);
        await _processor.StartProcessingAsync(stoppingToken);

        // Park here until shutdown is requested.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected: SIGTERM arrived and the host cancelled the token.
        }

        // Graceful shutdown. Container Apps sends SIGTERM and then waits before
        // killing the process, so stop pulling new messages and let whatever is
        // in flight finish. CancellationToken.None is deliberate — stoppingToken
        // is already cancelled, and passing it would abandon the current message.
        _logger.LogInformation("Shutdown requested; draining in-flight messages");
        await _processor.StopProcessingAsync(CancellationToken.None);
        _logger.LogInformation("Worker {Replica} stopped cleanly", _replicaName);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;

        _logger.LogInformation(
            "Received {MessageId} delivery {Delivery} on {Replica}",
            message.MessageId, message.DeliveryCount, _replicaName);

        ReportRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ReportRequest>(message.Body.ToString(), JsonOptions);
        }
        catch (JsonException ex)
        {
            request = null;
            _logger.LogWarning(ex, "Message {MessageId} is not valid JSON", message.MessageId);
        }

        if (request is null)
        {
            // A malformed payload will never succeed, so retrying it five times
            // only wastes compute. Dead-letter it immediately instead.
            await args.DeadLetterMessageAsync(
                message, "InvalidPayload", "Body did not deserialize to a ReportRequest",
                args.CancellationToken);

            _logger.LogWarning("Dead-lettered {MessageId}: invalid payload", message.MessageId);
            return;
        }

        await GenerateReportAsync(request, args.CancellationToken);

        // The message is only gone once this returns. Crash before it and the lock
        // expires, the message becomes visible again, and another replica retries.
        // After maxDeliveryCount (5) Service Bus dead-letters it automatically.
        await args.CompleteMessageAsync(message, args.CancellationToken);

        _logger.LogInformation("Completed {MessageId} for job {JobId}", message.MessageId, request.JobId);
    }

    private async Task GenerateReportAsync(ReportRequest request, CancellationToken ct)
    {
        if (string.Equals(request.Subject, "fail", StringComparison.OrdinalIgnoreCase))
        {
            // Deliberate failure path, so the retry and dead-letter behaviour can be
            // observed end to end. Five deliveries, then Service Bus parks it.
            throw new InvalidOperationException("Deliberate failure for demonstration");
        }

        _logger.LogInformation(
            "Generating report for {Subject}, {Seconds}s of work", request.Subject, request.WorkSeconds);

        await Task.Delay(TimeSpan.FromSeconds(request.WorkSeconds), ct);

        // A real worker would write the result to Blob Storage or a database here,
        // so the API could serve it from GET /reports/{id}.
        _logger.LogInformation("Report ready for {Subject}", request.Subject);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception, "Service Bus error during {Operation} on {Entity}",
            args.ErrorSource, args.EntityPath);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_processor is not null)
        {
            await _processor.DisposeAsync();
        }
    }
}
