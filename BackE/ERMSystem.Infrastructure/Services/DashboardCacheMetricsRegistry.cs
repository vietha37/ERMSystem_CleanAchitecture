using System.Collections.Concurrent;
using System.Threading;

namespace ERMSystem.Infrastructure.Services;

public class DashboardCacheMetricsRegistry
{
    private readonly ConcurrentDictionary<string, CacheMetricBucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

    public void RecordHit(string cacheSegment) => GetBucket(cacheSegment).RecordHit();

    public void RecordMiss(string cacheSegment) => GetBucket(cacheSegment).RecordMiss();

    public void RecordWrite(string cacheSegment) => GetBucket(cacheSegment).RecordWrite();

    public void RecordInvalidation() => GetBucket("all").RecordInvalidation();

    public IReadOnlyCollection<DashboardCacheMetricSnapshot> GetSnapshots()
        => _buckets
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Value.GetSnapshot(x.Key))
            .ToArray();

    private CacheMetricBucket GetBucket(string cacheSegment)
        => _buckets.GetOrAdd(string.IsNullOrWhiteSpace(cacheSegment) ? "unknown" : cacheSegment.Trim(), _ => new CacheMetricBucket());

    private sealed class CacheMetricBucket
    {
        private long _hits;
        private long _misses;
        private long _writes;
        private long _invalidations;

        public void RecordHit() => Interlocked.Increment(ref _hits);

        public void RecordMiss() => Interlocked.Increment(ref _misses);

        public void RecordWrite() => Interlocked.Increment(ref _writes);

        public void RecordInvalidation() => Interlocked.Increment(ref _invalidations);

        public DashboardCacheMetricSnapshot GetSnapshot(string cacheSegment)
            => new(
                cacheSegment,
                Volatile.Read(ref _hits),
                Volatile.Read(ref _misses),
                Volatile.Read(ref _writes),
                Volatile.Read(ref _invalidations));
    }
}

public sealed record DashboardCacheMetricSnapshot(
    string CacheSegment,
    long Hits,
    long Misses,
    long Writes,
    long Invalidations);
