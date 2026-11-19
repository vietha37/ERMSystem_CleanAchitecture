using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces;

public interface ICatalogQueryCache
{
    Task<HospitalCatalogOverviewDto?> GetOverviewAsync(CancellationToken ct = default);
    Task SetOverviewAsync(HospitalCatalogOverviewDto overview, CancellationToken ct = default);
    Task InvalidateAsync(CancellationToken ct = default);
}
