using System.Text;
using System.Text.Json;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using ERMSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ERMSystem.Infrastructure.Messaging;

public class HospitalNotificationConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<HospitalNotificationConsumerService> _logger;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly NotificationConsumerOptions _consumerOptions;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;

    private IConnection? _connection;
    private IModel? _channel;
    private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

    public HospitalNotificationConsumerService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<NotificationConsumerOptions> consumerOptions,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        ILogger<HospitalNotificationConsumerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _consumerOptions = consumerOptions.Value;
        _workerHealthRegistry = workerHealthRegistry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khoi dong worker consume notification tu RabbitMQ.");
        _workerHealthRegistry.Report("hospital-notification-consumer", "Starting", "Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            EnsureConsumer(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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

    private bool EnsureConsumer(CancellationToken stoppingToken)
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
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection("ERMSystem.HospitalNotificationConsumer");
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, _consumerOptions.PrefetchCount, false);

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

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, eventArgs) =>
            {
                await HandleMessageAsync(eventArgs, stoppingToken);
            };

            _channel.BasicConsume(
                queue: _rabbitMqOptions.NotificationQueue,
                autoAck: false,
                consumer: consumer);

            _nextConnectAttemptUtc = DateTime.MinValue;
            _workerHealthRegistry.Report("hospital-notification-consumer", "Healthy", "RabbitMQ consumer connected.", successAtUtc: nowUtc);

            _connection.ConnectionShutdown += (_, _) =>
            {
                _workerHealthRegistry.Report("hospital-notification-consumer", "Degraded", "RabbitMQ connection shutdown detected.");
                DisposeChannel();
            };

            return true;
        }
        catch (Exception ex)
        {
            _nextConnectAttemptUtc = nowUtc.AddSeconds(Math.Max(5, _consumerOptions.ReconnectDelaySeconds));
            _logger.LogWarning(ex, "Chua ket noi duoc RabbitMQ consumer. Se thu lai sau.");
            _workerHealthRegistry.Report("hospital-notification-consumer", "Degraded", ex.Message, errorAtUtc: nowUtc);
            DisposeChannel();
            return false;
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            return;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<RabbitMqEventEnvelope>(
                Encoding.UTF8.GetString(eventArgs.Body.ToArray()),
                JsonOptions);

            if (envelope == null || envelope.MessageId == Guid.Empty)
            {
                _logger.LogWarning("Nhan duoc message notification khong hop le, se ack bo qua.");
                _workerHealthRegistry.Report("hospital-notification-consumer", "Degraded", "Received invalid message envelope.");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var messageKey = envelope.MessageId.ToString();

            var alreadyProcessed = await hospitalDbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(x => x.SourceSystem == "RabbitMQ" && x.MessageKey == messageKey, stoppingToken);

            if (alreadyProcessed)
            {
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var templateCode = ResolveTemplateCode(envelope.EventType);
            var payload = envelope.Payload.Deserialize<AppointmentNotificationPayload>(JsonOptions);

            hospitalDbContext.InboxMessages.Add(new IntegrationInboxMessageEntity
            {
                Id = Guid.NewGuid(),
                SourceSystem = "RabbitMQ",
                MessageKey = messageKey,
                EventType = envelope.EventType,
                PayloadJson = JsonSerializer.Serialize(envelope, JsonOptions),
                ProcessedAtUtc = nowUtc,
                Status = "Processed"
            });

            if (payload != null)
            {
                var recipientTargets = BuildRecipientTargets(payload);

                foreach (var target in recipientTargets)
                {
                    var template = await hospitalDbContext.NotificationTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.TemplateCode == templateCode &&
                                 x.ChannelCode == target.ChannelCode &&
                                 x.IsActive,
                            stoppingToken);

                    if (template == null && !string.Equals(templateCode, "GENERIC_NOTIFICATION", StringComparison.Ordinal))
                    {
                        template = await hospitalDbContext.NotificationTemplates
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x => x.TemplateCode == "GENERIC_NOTIFICATION" &&
                                     x.ChannelCode == target.ChannelCode &&
                                     x.IsActive,
                                stoppingToken);
                    }

                    hospitalDbContext.NotificationDeliveries.Add(new HospitalNotificationDeliveryEntity
                    {
                        Id = Guid.NewGuid(),
                        OutboxMessageId = envelope.MessageId,
                        ChannelCode = target.ChannelCode,
                        Recipient = target.Recipient,
                        DeliveryStatus = template == null ? "Skipped" : "Queued",
                        AttemptCount = 0,
                        LastAttemptAtUtc = null,
                        DeliveredAtUtc = null,
                        ErrorMessage = template == null
                            ? $"Khong tim thay template {templateCode} cho kenh {target.ChannelCode}."
                            : null
                    });
                }
            }

            await hospitalDbContext.SaveChangesAsync(stoppingToken);
            _channel.BasicAck(eventArgs.DeliveryTag, false);
            _workerHealthRegistry.Report("hospital-notification-consumer", "Healthy", $"Processed event {envelope.EventType}.", successAtUtc: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer notification xu ly message that bai. Message se duoc requeue.");
            _workerHealthRegistry.Report("hospital-notification-consumer", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
            _channel.BasicNack(eventArgs.DeliveryTag, false, true);
        }
    }

    private static string ResolveTemplateCode(string eventType)
        => eventType switch
        {
            "AppointmentCreated.v1" => "APPOINTMENT_CREATED",
            "AppointmentUpdated.v1" => "APPOINTMENT_UPDATED",
            "AppointmentCancelled.v1" => "APPOINTMENT_CANCELLED",
            "MedicalRecordFinalized.v1" => "MEDICAL_RECORD_FINALIZED",
            "PrescriptionIssued.v1" => "PRESCRIPTION_ISSUED",
            "PrescriptionDispensed.v1" => "PRESCRIPTION_DISPENSED",
            "InvoiceIssued.v1" => "INVOICE_ISSUED",
            "InvoicePaymentReceived.v1" => "INVOICE_PAYMENT_RECEIVED",
            "InvoiceRefunded.v1" => "INVOICE_REFUNDED",
            "PatientRevisitReminder.v1" => "PATIENT_REVISIT_REMINDER",
            "PatientSatisfactionSurvey.v1" => "PATIENT_SATISFACTION_SURVEY",
            "PatientCustomerCareFollowUp.v1" => "PATIENT_CUSTOMER_CARE_FOLLOW_UP",
            _ => "GENERIC_NOTIFICATION"
        };

    private static IReadOnlyList<NotificationRecipientTarget> BuildRecipientTargets(AppointmentNotificationPayload payload)
    {
        var targets = new List<NotificationRecipientTarget>();

        if (!string.IsNullOrWhiteSpace(payload.Email))
        {
            targets.Add(new NotificationRecipientTarget("Email", payload.Email.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(payload.Phone))
        {
            targets.Add(new NotificationRecipientTarget("SMS", payload.Phone.Trim()));
        }

        return targets;
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

    private sealed record NotificationRecipientTarget(string ChannelCode, string Recipient);

    private sealed class RabbitMqEventEnvelope
    {
        public Guid MessageId { get; set; }
        public string AggregateType { get; set; } = string.Empty;
        public Guid AggregateId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public JsonElement Payload { get; set; }
    }

    private sealed class AppointmentNotificationPayload
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? MedicalRecordNumber { get; set; }
        public string? DiagnosisName { get; set; }
        public Guid DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public DateTime AppointmentStartLocal { get; set; }
        public DateTime? AppointmentEndLocal { get; set; }
        public DateTime? FinalizedAtUtc { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string? CurrentStatus { get; set; }
        public string? PrescriptionNumber { get; set; }
        public string? EncounterNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal? Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
