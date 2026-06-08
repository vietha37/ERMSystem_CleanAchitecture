namespace ERMSystem.API.Services;

public class OperationalAlertOptions
{
    public bool DispatchEnabled { get; set; }
    public int DispatchIntervalSeconds { get; set; } = 60;
    public int RepeatIntervalMinutes { get; set; } = 30;
    public bool SendResolvedAlerts { get; set; } = true;
    public int PendingOutboxWarningThreshold { get; set; } = 25;
    public int PendingOutboxCriticalThreshold { get; set; } = 100;
    public int QueuedDeliveryWarningThreshold { get; set; } = 25;
    public int QueuedDeliveryCriticalThreshold { get; set; } = 100;
    public int StaleQueuedWarningThreshold { get; set; } = 5;
    public int StaleQueuedCriticalThreshold { get; set; } = 20;
    public int OldestQueuedWarningMinutes { get; set; } = 15;
    public int OldestQueuedCriticalMinutes { get; set; } = 60;
    public int WorkerStaleWarningMinutes { get; set; } = 10;
    public int WorkerStaleCriticalMinutes { get; set; } = 20;
    public int CacheMinimumSamples { get; set; } = 10;
    public int CacheHitRatioWarningPercent { get; set; } = 60;
    public int CacheHitRatioCriticalPercent { get; set; } = 40;
    public OperationalAlertWebhookOptions Webhook { get; set; } = new();
}

public class OperationalAlertWebhookOptions
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? BearerToken { get; set; }
    public string? SigningSecret { get; set; }
    public string SignatureHeaderName { get; set; } = "X-ERM-Alert-Signature";
    public string TimestampHeaderName { get; set; } = "X-ERM-Alert-Timestamp";
    public int TimeoutSeconds { get; set; } = 10;
}
