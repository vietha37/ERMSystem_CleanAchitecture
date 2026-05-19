namespace ERMSystem.Infrastructure.Services;

public class DashboardCacheOptions
{
    public int StatsTtlSeconds { get; set; } = 60;
    public int TrendsTtlSeconds { get; set; } = 300;
}
