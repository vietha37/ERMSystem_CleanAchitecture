using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERMSystem.Infrastructure.Services;

public class NotificationPipelineMetricsReader
{
    private static readonly string[] TerminalDeliveryStatuses = ["Delivered", "Failed", "Skipped"];
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public NotificationPipelineMetricsReader(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<NotificationPipelineMetricsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var nowUtc = DateTime.UtcNow;
        var staleQueuedCutoffUtc = nowUtc.AddMinutes(-15);

        var pendingOutboxReferenceTimes = await hospitalDbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Status == "Pending")
            .Select(x => x.AvailableAtUtc)
            .ToListAsync(ct);

        var queuedDeliveryReferenceTimes = await hospitalDbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(x => !TerminalDeliveryStatuses.Contains(x.DeliveryStatus))
            .Select(x => x.LastAttemptAtUtc ?? x.OutboxMessage.PublishedAtUtc ?? x.OutboxMessage.AvailableAtUtc)
            .ToListAsync(ct);

        var oldestPendingOutboxAtUtc = pendingOutboxReferenceTimes.Count == 0
            ? (DateTime?)null
            : pendingOutboxReferenceTimes.Min();

        var oldestQueuedDeliveryAtUtc = queuedDeliveryReferenceTimes.Count == 0
            ? (DateTime?)null
            : queuedDeliveryReferenceTimes.Min();

        return new NotificationPipelineMetricsSnapshot
        {
            GeneratedAtUtc = nowUtc,
            PendingOutboxCount = pendingOutboxReferenceTimes.Count,
            OldestPendingOutboxAtUtc = oldestPendingOutboxAtUtc,
            QueuedDeliveryCount = queuedDeliveryReferenceTimes.Count,
            StaleQueuedDeliveryCount = queuedDeliveryReferenceTimes.Count(x => x < staleQueuedCutoffUtc),
            OldestQueuedDeliveryAtUtc = oldestQueuedDeliveryAtUtc
        };
    }
}

public class NotificationPipelineMetricsSnapshot
{
    public DateTime GeneratedAtUtc { get; set; }
    public int PendingOutboxCount { get; set; }
    public DateTime? OldestPendingOutboxAtUtc { get; set; }
    public int QueuedDeliveryCount { get; set; }
    public int StaleQueuedDeliveryCount { get; set; }
    public DateTime? OldestQueuedDeliveryAtUtc { get; set; }
}
