namespace ERMSystem.API.Services;

public class OperationalAlertNotificationEnvelope
{
    public DateTime GeneratedAtUtc { get; set; }
    public string EventType { get; set; } = "activated";
    public string OverallStatus { get; set; } = "warning";
    public OperationalAlertItem Alert { get; set; } = new();
}

public class OperationalAlertDispatchState
{
    public string Severity { get; set; } = "warning";
    public DateTime FirstSeenAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public OperationalAlertItem LastAlert { get; set; } = new();
}
