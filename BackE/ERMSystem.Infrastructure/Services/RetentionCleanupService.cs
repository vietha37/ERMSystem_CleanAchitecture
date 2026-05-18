using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class RetentionCleanupService : BackgroundService
{
    private static readonly string[] TerminalDeliveryStatuses = ["Delivered", "Failed", "Skipped"];

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<RetentionCleanupService> _logger;
    private readonly RetentionCleanupOptions _options;

    public RetentionCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RetentionCleanupOptions> options,
        ILogger<RetentionCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khoi dong worker retention cleanup.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, _options.PollIntervalHours)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention cleanup gap loi khong mong muon.");
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

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var nowUtc = DateTime.UtcNow;
        var batchSize = Math.Max(1, _options.BatchSize);

        var securityCutoffUtc = nowUtc.AddDays(-Math.Max(1, _options.SecurityEventsRetentionDays));
        var deliveryCutoffUtc = nowUtc.AddDays(-Math.Max(1, _options.NotificationDeliveriesRetentionDays));
        var outboxCutoffUtc = nowUtc.AddDays(-Math.Max(1, _options.PublishedOutboxRetentionDays));

        var deletedSecurityEvents = await DeleteSecurityEventsAsync(hospitalDbContext, securityCutoffUtc, batchSize, ct);
        var deletedDeliveries = await DeleteNotificationDeliveriesAsync(hospitalDbContext, deliveryCutoffUtc, batchSize, ct);
        var deletedOutbox = await DeletePublishedOutboxMessagesAsync(hospitalDbContext, outboxCutoffUtc, batchSize, ct);

        if (deletedSecurityEvents > 0 || deletedDeliveries > 0 || deletedOutbox > 0)
        {
            _logger.LogInformation(
                "Retention cleanup da xoa SecurityEvents={SecurityEventsCount}, NotificationDeliveries={DeliveryCount}, OutboxMessages={OutboxCount}.",
                deletedSecurityEvents,
                deletedDeliveries,
                deletedOutbox);
        }
    }

    private static async Task<int> DeleteSecurityEventsAsync(
        HospitalDbContext hospitalDbContext,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        var events = await hospitalDbContext.SecurityEvents
            .Where(x => x.OccurredAtUtc < cutoffUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            return 0;
        }

        hospitalDbContext.SecurityEvents.RemoveRange(events);
        await hospitalDbContext.SaveChangesAsync(ct);
        return events.Count;
    }

    private static async Task<int> DeleteNotificationDeliveriesAsync(
        HospitalDbContext hospitalDbContext,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        var deliveries = await hospitalDbContext.NotificationDeliveries
            .Where(x => TerminalDeliveryStatuses.Contains(x.DeliveryStatus))
            .Where(x =>
                (x.DeliveredAtUtc.HasValue && x.DeliveredAtUtc.Value < cutoffUtc) ||
                (!x.DeliveredAtUtc.HasValue && x.LastAttemptAtUtc.HasValue && x.LastAttemptAtUtc.Value < cutoffUtc))
            .OrderBy(x => x.DeliveredAtUtc ?? x.LastAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        if (deliveries.Count == 0)
        {
            return 0;
        }

        hospitalDbContext.NotificationDeliveries.RemoveRange(deliveries);
        await hospitalDbContext.SaveChangesAsync(ct);
        return deliveries.Count;
    }

    private static async Task<int> DeletePublishedOutboxMessagesAsync(
        HospitalDbContext hospitalDbContext,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        var messages = await hospitalDbContext.OutboxMessages
            .Where(x => x.Status == "Published" && x.PublishedAtUtc.HasValue && x.PublishedAtUtc.Value < cutoffUtc)
            .Where(x => !hospitalDbContext.NotificationDeliveries.Any(d => d.OutboxMessageId == x.Id))
            .OrderBy(x => x.PublishedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
        {
            return 0;
        }

        hospitalDbContext.OutboxMessages.RemoveRange(messages);
        await hospitalDbContext.SaveChangesAsync(ct);
        return messages.Count;
    }
}
