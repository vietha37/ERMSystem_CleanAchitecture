namespace ERMSystem.Infrastructure.Services;

public class SatisfactionSurveyOptions
{
    public int PollIntervalHours { get; set; } = 24;
    public int SurveyDelayHours { get; set; } = 6;
    public int SurveyCooldownDays { get; set; } = 30;
    public int MaxCompletedLookbackDays { get; set; } = 14;
    public int BatchSize { get; set; } = 50;
}
