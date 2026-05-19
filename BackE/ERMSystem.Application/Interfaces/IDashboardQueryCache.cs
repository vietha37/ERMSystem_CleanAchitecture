using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces;

public interface IDashboardQueryCache
{
    Task<DashboardStatsDto?> GetStatsAsync(CancellationToken ct = default);
    Task SetStatsAsync(DashboardStatsDto value, CancellationToken ct = default);
    Task<DashboardTrendsDto?> GetTrendsAsync(string period, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task SetTrendsAsync(string period, DateTime fromDate, DateTime toDate, DashboardTrendsDto value, CancellationToken ct = default);
    Task InvalidateAsync(CancellationToken ct = default);
}
