using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Messaging;

public class HospitalNotificationDispatchService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEnumerable<INotificationChannelSender> _senders;
    private readonly NotificationDispatchOptions _options;
    private readonly ILogger<HospitalNotificationDispatchService> _logger;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;

    public HospitalNotificationDispatchService(
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<INotificationChannelSender> senders,
        IOptions<NotificationDispatchOptions> options,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        ILogger<HospitalNotificationDispatchService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _senders = senders;
        _options = options.Value;
        _workerHealthRegistry = workerHealthRegistry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khoi dong worker dispatch NotificationDeliveries.");
        _workerHealthRegistry.Report("hospital-notification-dispatch", "Starting", "Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedDeliveriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker dispatch delivery gap loi khong mong muon.");
                _workerHealthRegistry.Report("hospital-notification-dispatch", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
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

    private async Task ProcessQueuedDeliveriesAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var nowUtc = DateTime.UtcNow;

        var deliveries = await hospitalDbContext.NotificationDeliveries
            .Where(x => x.DeliveryStatus == "Queued")
            .OrderBy(x => x.LastAttemptAtUtc ?? x.DeliveredAtUtc)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(ct);

        if (deliveries.Count == 0)
        {
            _workerHealthRegistry.Report("hospital-notification-dispatch", "Healthy", "No queued deliveries.", successAtUtc: nowUtc);
            return;
        }

        foreach (var delivery in deliveries)
        {
            delivery.AttemptCount += 1;
            delivery.LastAttemptAtUtc = nowUtc;

            var sender = _senders.FirstOrDefault(x => string.Equals(x.ChannelCode, delivery.ChannelCode, StringComparison.OrdinalIgnoreCase));
            if (sender == null)
            {
                delivery.DeliveryStatus = "Failed";
                delivery.ErrorMessage = $"Khong tim thay sender cho kenh {delivery.ChannelCode}.";
                _workerHealthRegistry.Report("hospital-notification-dispatch", "Degraded", $"Missing sender for channel {delivery.ChannelCode}.", errorAtUtc: nowUtc);
                continue;
            }

            var result = await sender.SendAsync(new NotificationSendRequest
            {
                DeliveryId = delivery.Id,
                OutboxMessageId = delivery.OutboxMessageId,
                Recipient = delivery.Recipient,
                ChannelCode = delivery.ChannelCode
            }, ct);

            if (result.Success)
            {
                delivery.DeliveryStatus = "Delivered";
                delivery.DeliveredAtUtc = nowUtc;
                delivery.ProviderMessageId = result.ProviderMessageId;
                delivery.ErrorMessage = null;
            }
            else
            {
                delivery.DeliveryStatus = "Failed";
                delivery.ErrorMessage = result.ErrorMessage ?? "Khong ro nguyen nhan gui that bai.";
            }
        }

        await hospitalDbContext.SaveChangesAsync(ct);
        _workerHealthRegistry.Report("hospital-notification-dispatch", "Healthy", $"Processed queued deliveries batch size={deliveries.Count}.", successAtUtc: nowUtc);
    }
}
