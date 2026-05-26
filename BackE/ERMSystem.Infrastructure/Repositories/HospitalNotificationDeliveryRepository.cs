using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalNotificationDeliveryRepository : IHospitalNotificationDeliveryRepository
{
    private static readonly string[] CrmEventTypes =
    [
        "PatientRevisitReminder.v1",
        "PatientSatisfactionSurvey.v1",
        "PatientCustomerCareFollowUp.v1"
    ];

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

    public async Task<HospitalCrmEngagementSummaryDto> GetEngagementSummaryAsync(CancellationToken ct = default)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var trendFromDate = generatedAtUtc.Date.AddDays(-13);

        var crmOutboxMessages = await _hospitalDbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.AggregateType == "Patient")
            .Where(x => CrmEventTypes.Contains(x.EventType))
            .Select(x => new CrmOutboxMessageSnapshot
            {
                Id = x.Id,
                AggregateId = x.AggregateId,
                EventType = x.EventType,
                PayloadJson = x.PayloadJson,
                AvailableAtUtc = x.AvailableAtUtc,
                PublishedAtUtc = x.PublishedAtUtc
            })
            .ToListAsync(ct);

        if (crmOutboxMessages.Count == 0)
        {
            return new HospitalCrmEngagementSummaryDto
            {
                GeneratedAtUtc = generatedAtUtc,
                TrendPoints = BuildTrendPoints(trendFromDate, generatedAtUtc.Date, Array.Empty<CrmOutboxMessageSnapshot>()),
                RecentActivities = Array.Empty<HospitalCrmEngagementActivityDto>()
            };
        }

        var outboxMessageIds = crmOutboxMessages.Select(x => x.Id).ToArray();
        var deliveries = await _hospitalDbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(x => outboxMessageIds.Contains(x.OutboxMessageId))
            .Select(x => new DeliveryStatusSnapshot
            {
                OutboxMessageId = x.OutboxMessageId,
                ChannelCode = x.ChannelCode,
                DeliveryStatus = x.DeliveryStatus
            })
            .ToListAsync(ct);

        var deliveriesByOutboxMessage = deliveries
            .GroupBy(x => x.OutboxMessageId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var recentActivities = crmOutboxMessages
            .OrderByDescending(x => x.AvailableAtUtc)
            .Take(12)
            .Select(message =>
            {
                deliveriesByOutboxMessage.TryGetValue(message.Id, out var messageDeliveries);
                messageDeliveries ??= Array.Empty<DeliveryStatusSnapshot>();
                var payload = ParsePayload(message.PayloadJson);

                return new HospitalCrmEngagementActivityDto
                {
                    OutboxMessageId = message.Id,
                    PatientId = message.AggregateId,
                    EventType = message.EventType,
                    EventLabel = ResolveCrmEventLabel(message.EventType),
                    PatientName = payload.PatientName ?? "Khong ro benh nhan",
                    MedicalRecordNumber = payload.MedicalRecordNumber,
                    ClinicName = payload.ClinicName,
                    DoctorName = payload.DoctorName,
                    Channel = payload.Channel,
                    RecipientCount = messageDeliveries.Length,
                    QueuedCount = messageDeliveries.Count(x => string.Equals(x.DeliveryStatus, "Queued", StringComparison.OrdinalIgnoreCase)),
                    DeliveredCount = messageDeliveries.Count(x => string.Equals(x.DeliveryStatus, "Delivered", StringComparison.OrdinalIgnoreCase)),
                    FailedCount = messageDeliveries.Count(x => string.Equals(x.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase)),
                    SkippedCount = messageDeliveries.Count(x => string.Equals(x.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase)),
                    AvailableAtUtc = message.AvailableAtUtc,
                    PublishedAtUtc = message.PublishedAtUtc
                };
            })
            .ToArray();

        return new HospitalCrmEngagementSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            TotalCampaignMessages = crmOutboxMessages.Count,
            TotalRecipients = deliveries.Count,
            QueuedDeliveries = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Queued", StringComparison.OrdinalIgnoreCase)),
            DeliveredDeliveries = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Delivered", StringComparison.OrdinalIgnoreCase)),
            FailedDeliveries = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase)),
            SkippedDeliveries = deliveries.Count(x => string.Equals(x.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase)),
            ActionRequiredDeliveries = deliveries.Count(x =>
                string.Equals(x.DeliveryStatus, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.DeliveryStatus, "Skipped", StringComparison.OrdinalIgnoreCase)),
            RevisitReminderMessages = crmOutboxMessages.Count(x => x.EventType == "PatientRevisitReminder.v1"),
            SatisfactionSurveyMessages = crmOutboxMessages.Count(x => x.EventType == "PatientSatisfactionSurvey.v1"),
            CustomerCareFollowUpMessages = crmOutboxMessages.Count(x => x.EventType == "PatientCustomerCareFollowUp.v1"),
            TrendPoints = BuildTrendPoints(trendFromDate, generatedAtUtc.Date, crmOutboxMessages),
            RecentActivities = recentActivities
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

    private static HospitalCrmEngagementTrendPointDto[] BuildTrendPoints(
        DateTime fromDate,
        DateTime toDate,
        IEnumerable<CrmOutboxMessageSnapshot> crmOutboxMessages)
    {
        var grouped = crmOutboxMessages
            .GroupBy(x => new { Date = ((DateTime)x.AvailableAtUtc).Date, EventType = (string)x.EventType })
            .ToDictionary(x => (x.Key.Date, x.Key.EventType), x => x.Count());

        var points = new List<HospitalCrmEngagementTrendPointDto>();
        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            grouped.TryGetValue((date, "PatientRevisitReminder.v1"), out var revisitCount);
            grouped.TryGetValue((date, "PatientSatisfactionSurvey.v1"), out var satisfactionCount);
            grouped.TryGetValue((date, "PatientCustomerCareFollowUp.v1"), out var followUpCount);

            points.Add(new HospitalCrmEngagementTrendPointDto
            {
                Date = date,
                Label = date.ToString("dd/MM"),
                RevisitReminderCount = revisitCount,
                SatisfactionSurveyCount = satisfactionCount,
                CustomerCareFollowUpCount = followUpCount
            });
        }

        return points.ToArray();
    }

    private static string ResolveCrmEventLabel(string eventType)
        => eventType switch
        {
            "PatientRevisitReminder.v1" => "Nhac tai kham",
            "PatientSatisfactionSurvey.v1" => "Khao sat hai long",
            "PatientCustomerCareFollowUp.v1" => "CSKH sau kham",
            _ => eventType
        };

    private static CrmPayloadSnapshot ParsePayload(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            return new CrmPayloadSnapshot
            {
                PatientName = TryGetString(root, "patientName"),
                MedicalRecordNumber = TryGetString(root, "medicalRecordNumber"),
                ClinicName = TryGetString(root, "clinicName"),
                DoctorName = TryGetString(root, "doctorName"),
                Channel = TryGetString(root, "channel")
            };
        }
        catch
        {
            return new CrmPayloadSnapshot();
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private sealed class CrmPayloadSnapshot
    {
        public string? PatientName { get; set; }
        public string? MedicalRecordNumber { get; set; }
        public string? ClinicName { get; set; }
        public string? DoctorName { get; set; }
        public string? Channel { get; set; }
    }

    private sealed class CrmOutboxMessageSnapshot
    {
        public Guid Id { get; set; }
        public Guid AggregateId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime AvailableAtUtc { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
    }

    private sealed class DeliveryStatusSnapshot
    {
        public Guid OutboxMessageId { get; set; }
        public string ChannelCode { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
    }
}
