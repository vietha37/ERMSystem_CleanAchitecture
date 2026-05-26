namespace ERMSystem.Application.DTOs;

public class HospitalCrmEngagementSummaryDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalCampaignMessages { get; set; }
    public int TotalRecipients { get; set; }
    public int QueuedDeliveries { get; set; }
    public int DeliveredDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public int SkippedDeliveries { get; set; }
    public int ActionRequiredDeliveries { get; set; }
    public int RevisitReminderMessages { get; set; }
    public int SatisfactionSurveyMessages { get; set; }
    public int CustomerCareFollowUpMessages { get; set; }
    public IReadOnlyCollection<HospitalCrmEngagementTrendPointDto> TrendPoints { get; set; } = Array.Empty<HospitalCrmEngagementTrendPointDto>();
    public IReadOnlyCollection<HospitalCrmEngagementActivityDto> RecentActivities { get; set; } = Array.Empty<HospitalCrmEngagementActivityDto>();
}

public class HospitalCrmEngagementTrendPointDto
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int RevisitReminderCount { get; set; }
    public int SatisfactionSurveyCount { get; set; }
    public int CustomerCareFollowUpCount { get; set; }
}

public class HospitalCrmEngagementActivityDto
{
    public Guid OutboxMessageId { get; set; }
    public Guid PatientId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventLabel { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? MedicalRecordNumber { get; set; }
    public string? ClinicName { get; set; }
    public string? DoctorName { get; set; }
    public string? Channel { get; set; }
    public int RecipientCount { get; set; }
    public int QueuedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
