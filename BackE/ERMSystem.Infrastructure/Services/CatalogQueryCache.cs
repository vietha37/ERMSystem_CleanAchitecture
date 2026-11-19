using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace ERMSystem.Infrastructure.Services;

public class CatalogQueryCache : ICatalogQueryCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string VersionKey = "catalog:version:v1";
    private const string OverviewKeyPrefix = "catalog:overview";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _distributedCache;

    public CatalogQueryCache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<HospitalCatalogOverviewDto?> GetOverviewAsync(CancellationToken ct = default)
    {
        var key = await BuildKeyAsync(ct);
        var payload = await _distributedCache.GetStringAsync(key, ct);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<HospitalCatalogOverviewDto>(payload, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetOverviewAsync(HospitalCatalogOverviewDto overview, CancellationToken ct = default)
    {
        var key = await BuildKeyAsync(ct);
        var payload = JsonSerializer.Serialize(overview, SerializerOptions);
        await _distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DefaultTtl },
            ct);
    }

    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        var currentVersion = await GetCurrentVersionAsync(ct);
        await _distributedCache.SetStringAsync(VersionKey, (currentVersion + 1).ToString(), ct);
    }

    private async Task<string> BuildKeyAsync(CancellationToken ct)
    {
        var version = await GetCurrentVersionAsync(ct);
        return $"{OverviewKeyPrefix}:{version}";
    }

    private async Task<long> GetCurrentVersionAsync(CancellationToken ct)
    {
        var rawVersion = await _distributedCache.GetStringAsync(VersionKey, ct);
        if (long.TryParse(rawVersion, out var version) && version > 0)
        {
            return version;
        }

        await _distributedCache.SetStringAsync(VersionKey, "1", ct);
        return 1;
    }
}
