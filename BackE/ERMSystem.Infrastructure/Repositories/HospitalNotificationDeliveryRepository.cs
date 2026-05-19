using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalNotificationDeliveryRepository : IHospitalNotificationDeliveryRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalNotificationDeliveryRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<NotificationDeliveryListDto> GetDeliveriesAsync(
        string? status,
        string? channelCode,
        string? recipient,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _hospitalDbContext.NotificationDeliveries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.DeliveryStatus == status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(channelCode))
        {
            var normalizedChannel = channelCode.Trim();
            query = query.Where(x => x.ChannelCode == normalizedChannel);
        }

        if (!string.IsNullOrWhiteSpace(recipient))
        {
            var recipientPattern = $"%{recipient.Trim()}%";
            query = query.Where(x => EF.Functions.Like(x.Recipient, recipientPattern));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.DeliveredAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationDeliveryDto
            {
                Id = x.Id,
                OutboxMessageId = x.OutboxMessageId,
                ChannelCode = x.ChannelCode,
                Recipient = x.Recipient,
                DeliveryStatus = x.DeliveryStatus,
                ProviderMessageId = x.ProviderMessageId,
                AttemptCount = x.AttemptCount,
                LastAttemptAtUtc = x.LastAttemptAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync(ct);

        return new NotificationDeliveryListDto
        {
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<NotificationDeliverySummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var staleQueuedCutoffUtc = nowUtc.AddMinutes(-15);
        var deliveries = await _hospitalDbContext.NotificationDeliveries
            .AsNoTracking()
            .Select(x => new
            {
                x.DeliveryStatus,
                x.LastAttemptAtUtc,
                x.DeliveredAtUtc,
                QueueReferenceAtUtc = x.LastAttemptAtUtc ?? x.OutboxMessage.PublishedAtUtc ?? x.OutboxMessage.AvailableAtUtc
            })
            .ToListAsync(ct);

        var queuedItems = deliveries
            .Where(x => string.Equals(x.DeliveryStatus, "Queued", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return new NotificationDeliverySummaryDto
        {
            TotalCount = deliveries.Count,
            QueuedCount = queuedItems.Length,
            DeliveredCount = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Delivered", StringComparison.OrdinalIgnoreCase)),
            FailedCount = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase)),
            SkippedCount = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase)),
            ActionRequiredCount = deliveries.Count(x =>
                string.Equals(x.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase)),
            StaleQueuedCount = queuedItems.Count(x =>
                x.QueueReferenceAtUtc < staleQueuedCutoffUtc),
            OldestQueuedAtUtc = queuedItems
                .Select(x => x.QueueReferenceAtUtc)
                .OrderBy(x => x)
                .FirstOrDefault(),
            GeneratedAtUtc = nowUtc
        };
    }

    public async Task<NotificationDeliveryRetryResult> RetryDeliveryAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var delivery = await _hospitalDbContext.NotificationDeliveries.FirstOrDefaultAsync(x => x.Id == deliveryId, ct);
        if (delivery == null)
        {
            return NotificationDeliveryRetryResult.NotFound;
        }

        if (!string.Equals(delivery.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(delivery.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationDeliveryRetryResult.InvalidStatus;
        }

        delivery.DeliveryStatus = "Queued";
        delivery.ErrorMessage = null;
        delivery.ProviderMessageId = null;
        delivery.DeliveredAtUtc = null;

        await _hospitalDbContext.SaveChangesAsync(ct);
        return NotificationDeliveryRetryResult.Requeued;
    }
}
