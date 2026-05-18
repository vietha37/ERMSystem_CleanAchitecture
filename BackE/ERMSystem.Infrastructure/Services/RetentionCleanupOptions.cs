namespace ERMSystem.Infrastructure.Services;

public class RetentionCleanupOptions
{
    public int PollIntervalHours { get; set; } = 24;
    public int SecurityEventsRetentionDays { get; set; } = 180;
    public int NotificationDeliveriesRetentionDays { get; set; } = 90;
    public int PublishedOutboxRetentionDays { get; set; } = 30;
    public int BatchSize { get; set; } = 500;
}
