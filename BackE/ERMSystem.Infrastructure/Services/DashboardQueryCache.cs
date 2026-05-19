using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class DashboardQueryCache : IDashboardQueryCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string VersionKey = "dashboard:version:v1";

    private readonly IDistributedCache _distributedCache;
    private readonly DashboardCacheOptions _options;
    private readonly DashboardCacheMetricsRegistry _metricsRegistry;

    public DashboardQueryCache(
        IDistributedCache distributedCache,
        IOptions<DashboardCacheOptions> options,
        DashboardCacheMetricsRegistry metricsRegistry)
    {
        _distributedCache = distributedCache;
        _options = options.Value;
        _metricsRegistry = metricsRegistry;
    }

    public async Task<DashboardStatsDto?> GetStatsAsync(CancellationToken ct = default)
    {
        var payload = await _distributedCache.GetStringAsync(await BuildStatsKeyAsync(ct), ct);
        var value = Deserialize<DashboardStatsDto>(payload);
        RecordRead("stats", value is not null);
        return value;
    }

    public async Task SetStatsAsync(DashboardStatsDto value, CancellationToken ct = default)
    {
        await SetAsync(await BuildStatsKeyAsync(ct), value, GetTtl(_options.StatsTtlSeconds), ct);
        _metricsRegistry.RecordWrite("stats");
    }

    public async Task<DashboardTrendsDto?> GetTrendsAsync(
        string period,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        var key = await BuildTrendsKeyAsync(period, fromDate, toDate, ct);
        var payload = await _distributedCache.GetStringAsync(key, ct);
        var value = Deserialize<DashboardTrendsDto>(payload);
        RecordRead("trends", value is not null);
        return value;
    }

    public async Task SetTrendsAsync(
        string period,
        DateTime fromDate,
        DateTime toDate,
        DashboardTrendsDto value,
        CancellationToken ct = default)
    {
        var key = await BuildTrendsKeyAsync(period, fromDate, toDate, ct);
        await SetAsync(key, value, GetTtl(_options.TrendsTtlSeconds), ct);
        _metricsRegistry.RecordWrite("trends");
    }

    public Task InvalidateAsync(CancellationToken ct = default)
    {
        var nextVersion = DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _metricsRegistry.RecordInvalidation();
        return _distributedCache.SetStringAsync(
            VersionKey,
            nextVersion,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            },
            ct);
    }

    private async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await _distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            ct);
    }

    private static T? Deserialize<T>(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    private async Task<string> BuildStatsKeyAsync(CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        return $"dashboard:stats:v1:{version}";
    }

    private async Task<string> BuildTrendsKeyAsync(string period, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        return $"dashboard:trends:v1:{version}:{period.ToLowerInvariant()}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
    }

    private async Task<string> GetVersionAsync(CancellationToken ct)
    {
        var version = await _distributedCache.GetStringAsync(VersionKey, ct);
        return string.IsNullOrWhiteSpace(version) ? "0" : version;
    }

    private static string BuildTrendsKey(string period, DateTime fromDate, DateTime toDate)
    {
        return $"dashboard:trends:v1:{period.ToLowerInvariant()}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
    }

    private static TimeSpan GetTtl(int ttlSeconds)
    {
        return TimeSpan.FromSeconds(Math.Max(1, ttlSeconds));
    }

    private void RecordRead(string cacheSegment, bool cacheHit)
    {
        if (cacheHit)
        {
            _metricsRegistry.RecordHit(cacheSegment);
            return;
        }

        _metricsRegistry.RecordMiss(cacheSegment);
    }
}
