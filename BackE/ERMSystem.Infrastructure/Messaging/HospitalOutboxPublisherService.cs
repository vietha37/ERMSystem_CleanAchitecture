using System.Text;
using System.Text.Json;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ERMSystem.Infrastructure.Messaging;

public class HospitalOutboxPublisherService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<HospitalOutboxPublisherService> _logger;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly OutboxPublisherOptions _publisherOptions;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;

    private IConnection? _connection;
    private IModel? _channel;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

    public HospitalOutboxPublisherService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<OutboxPublisherOptions> publisherOptions,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        ILogger<HospitalOutboxPublisherService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _publisherOptions = publisherOptions.Value;
        _workerHealthRegistry = workerHealthRegistry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khoi dong worker publish outbox sang RabbitMQ.");
        _workerHealthRegistry.Report("hospital-outbox-publisher", "Starting", "Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _publisherOptions.PollIntervalSeconds)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker publish outbox gap loi khong mong muon.");
                _workerHealthRegistry.Report("hospital-outbox-publisher", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
                DisposeChannel();
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public override void Dispose()
    {
        DisposeChannel();
        base.Dispose();
    }

    private async Task PublishPendingMessagesAsync(CancellationToken ct)
    {
        if (!EnsureChannel())
        {
            _workerHealthRegistry.Report("hospital-outbox-publisher", "Degraded", "RabbitMQ channel is not available.");
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var nowUtc = DateTime.UtcNow;

        var pendingMessages = await hospitalDbContext.OutboxMessages
            .Where(x => x.Status == "Pending" && x.AvailableAtUtc <= nowUtc)
            .OrderBy(x => x.AvailableAtUtc)
            .Take(Math.Max(1, _publisherOptions.BatchSize))
            .ToListAsync(ct);

        if (pendingMessages.Count == 0)
        {
            _workerHealthRegistry.Report("hospital-outbox-publisher", "Healthy", "No pending outbox messages.");
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                var routingKey = BuildRoutingKey(message.EventType);
                var payload = BuildEnvelope(message, nowUtc);
                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = message.Id.ToString();
                properties.Type = message.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: _rabbitMqOptions.Exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: payload);

                message.Status = "Published";
                message.PublishedAtUtc = nowUtc;
            }
            catch (Exception ex)
            {
                message.Status = "Pending";
                message.AvailableAtUtc = nowUtc.AddSeconds(Math.Max(5, _publisherOptions.RetryDelaySeconds));

                _logger.LogWarning(
                    ex,
                    "Khong the publish outbox message {MessageId} ({EventType}). Se thu lai sau.",
                    message.Id,
                    message.EventType);

                _workerHealthRegistry.Report("hospital-outbox-publisher", "Degraded", ex.Message, errorAtUtc: nowUtc);
                DisposeChannel();
                break;
            }
        }

        await hospitalDbContext.SaveChangesAsync(ct);
        _workerHealthRegistry.Report("hospital-outbox-publisher", "Healthy", $"Published batch size={pendingMessages.Count}.", successAtUtc: nowUtc);
    }

    private bool EnsureChannel()
    {
        if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
        {
            return true;
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc < _nextConnectAttemptUtc)
        {
            return false;
        }

        try
        {
            DisposeChannel();

            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.Host,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.Username,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost,
                DispatchConsumersAsync = false,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection("ERMSystem.HospitalOutboxPublisher");
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _rabbitMqOptions.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: _rabbitMqOptions.NotificationQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: _rabbitMqOptions.NotificationQueue,
                exchange: _rabbitMqOptions.Exchange,
                routingKey: _rabbitMqOptions.NotificationRoutingPattern);

            _workerHealthRegistry.Report("hospital-outbox-publisher", "Healthy", "RabbitMQ channel connected.", successAtUtc: nowUtc);
            return true;
        }
        catch (Exception ex)
        {
            _nextConnectAttemptUtc = nowUtc.AddSeconds(Math.Max(5, _publisherOptions.RetryDelaySeconds));
            _logger.LogWarning(ex, "Chua ket noi duoc RabbitMQ worker. Outbox se tiep tuc cho retry.");
            _workerHealthRegistry.Report("hospital-outbox-publisher", "Degraded", ex.Message, errorAtUtc: nowUtc);
            DisposeChannel();
            return false;
        }
    }

    private static byte[] BuildEnvelope(
        ERMSystem.Infrastructure.HospitalData.Entities.HospitalOutboxMessageEntity message,
        DateTime occurredAtUtc)
    {
        using var payloadDocument = JsonDocument.Parse(message.PayloadJson);

        var envelope = new
        {
            messageId = message.Id,
            aggregateType = message.AggregateType,
            aggregateId = message.AggregateId,
            eventType = message.EventType,
            occurredAtUtc,
            payload = payloadDocument.RootElement.Clone()
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));
    }

    private static string BuildRoutingKey(string eventType)
    {
        var normalized = eventType
            .Replace("Created", ".created", StringComparison.Ordinal)
            .Replace("Updated", ".updated", StringComparison.Ordinal)
            .Replace("Cancelled", ".cancelled", StringComparison.Ordinal)
            .Replace("Issued", ".issued", StringComparison.Ordinal)
            .Replace("Finalized", ".finalized", StringComparison.Ordinal)
            .Replace(".v", ".v", StringComparison.Ordinal)
            .Trim('.');

        normalized = normalized
            .Replace("..", ".", StringComparison.Ordinal)
            .Replace("V1", "v1", StringComparison.Ordinal)
            .Replace("V2", "v2", StringComparison.Ordinal);

        return normalized
            .Select(ch => char.IsUpper(ch) ? "." + char.ToLowerInvariant(ch) : ch.ToString())
            .Aggregate(string.Empty, (current, next) => current + next)
            .Trim('.')
            .Replace("..", ".", StringComparison.Ordinal);
    }

    private void DisposeChannel()
    {
        try
        {
            _channel?.Dispose();
        }
        catch
        {
            // Ignore dispose failures.
        }
        finally
        {
            _channel = null;
        }

        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // Ignore dispose failures.
        }
        finally
        {
            _connection = null;
        }
    }
}
