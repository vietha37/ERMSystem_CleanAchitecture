namespace ERMSystem.Infrastructure.Services;

public class CustomerCareFollowUpOptions
{
    public int PollIntervalHours { get; set; } = 24;
    public int FollowUpDelayHours { get; set; } = 24;
    public int FollowUpCooldownDays { get; set; } = 30;
    public int MaxCompletedLookbackDays { get; set; } = 21;
    public int BatchSize { get; set; } = 50;
}
