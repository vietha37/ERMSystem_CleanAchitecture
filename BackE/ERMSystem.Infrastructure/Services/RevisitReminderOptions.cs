namespace ERMSystem.Infrastructure.Services;

public class RevisitReminderOptions
{
    public int PollIntervalHours { get; set; } = 24;
    public int RevisitAfterDays { get; set; } = 30;
    public int ReminderCooldownDays { get; set; } = 21;
    public int UpcomingAppointmentWindowDays { get; set; } = 30;
    public int BatchSize { get; set; } = 50;
}
