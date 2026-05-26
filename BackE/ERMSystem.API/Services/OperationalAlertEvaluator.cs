using ERMSystem.Infrastructure.Services;

namespace ERMSystem.API.Services;

public class OperationalAlertEvaluator
{
    private readonly NotificationPipelineMetricsReader _notificationPipelineMetricsReader;
    private readonly BackgroundWorkerHealthRegistry _backgroundWorkerHealthRegistry;
    private readonly DashboardCacheMetricsRegistry _dashboardCacheMetricsRegistry;
    private readonly DistributedCacheRuntimeInfo _distributedCacheRuntimeInfo;
    private readonly OperationalAlertOptions _options;

    public OperationalAlertEvaluator(
        NotificationPipelineMetricsReader notificationPipelineMetricsReader,
        BackgroundWorkerHealthRegistry backgroundWorkerHealthRegistry,
        DashboardCacheMetricsRegistry dashboardCacheMetricsRegistry,
        DistributedCacheRuntimeInfo distributedCacheRuntimeInfo,
        IConfiguration configuration)
    {
        _notificationPipelineMetricsReader = notificationPipelineMetricsReader;
        _backgroundWorkerHealthRegistry = backgroundWorkerHealthRegistry;
        _dashboardCacheMetricsRegistry = dashboardCacheMetricsRegistry;
        _distributedCacheRuntimeInfo = distributedCacheRuntimeInfo;
        _options = configuration.GetSection("OperationalAlerts").Get<OperationalAlertOptions>() ?? new OperationalAlertOptions();
    }

    public async Task<OperationalAlertSnapshot> EvaluateAsync(CancellationToken ct = default)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var alerts = new List<OperationalAlertItem>();

        EvaluateDistributedCache(alerts);
        await EvaluateNotificationPipelineAsync(alerts, ct);
        EvaluateBackgroundWorkers(alerts, generatedAtUtc);
        EvaluateDashboardCache(alerts);

        return new OperationalAlertSnapshot
        {
            GeneratedAtUtc = generatedAtUtc,
            Status = ResolveOverallStatus(alerts),
            Alerts = alerts
                .OrderByDescending(x => x.Severity, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private void EvaluateDistributedCache(ICollection<OperationalAlertItem> alerts)
    {
        if (_distributedCacheRuntimeInfo.IsFallback)
        {
            alerts.Add(new OperationalAlertItem
            {
                Code = "distributed-cache-fallback",
                Severity = "warning",
                Message = "Distributed cache dang chay o che do fallback.",
                Metadata = new Dictionary<string, object?>
                {
                    ["provider"] = _distributedCacheRuntimeInfo.Provider,
                    ["reason"] = _distributedCacheRuntimeInfo.Reason
                }
            });
        }
    }

    private async Task EvaluateNotificationPipelineAsync(ICollection<OperationalAlertItem> alerts, CancellationToken ct)
    {
        var snapshot = await _notificationPipelineMetricsReader.GetSnapshotAsync(ct);
        var oldestQueuedAgeMinutes = snapshot.OldestQueuedDeliveryAtUtc.HasValue
            ? Math.Max(0, Math.Round((snapshot.GeneratedAtUtc - snapshot.OldestQueuedDeliveryAtUtc.Value).TotalMinutes, 2))
            : 0;

        AddThresholdAlert(
            alerts,
            "notification-outbox-pending",
            snapshot.PendingOutboxCount,
            _options.PendingOutboxWarningThreshold,
            _options.PendingOutboxCriticalThreshold,
            "Outbox notification dang ton qua nguong.",
            "Pending outbox notification dang cao.",
            "critical",
            "warning");

        AddThresholdAlert(
            alerts,
            "notification-deliveries-queued",
            snapshot.QueuedDeliveryCount,
            _options.QueuedDeliveryWarningThreshold,
            _options.QueuedDeliveryCriticalThreshold,
            "Notification delivery queued dang ton qua nguong.",
            "Queued notification delivery dang cao.",
            "critical",
            "warning");

        AddThresholdAlert(
            alerts,
            "notification-deliveries-stale",
            snapshot.StaleQueuedDeliveryCount,
            _options.StaleQueuedWarningThreshold,
            _options.StaleQueuedCriticalThreshold,
            "Notification delivery bi tre qua nguong critical.",
            "Notification delivery bi tre qua nguong warning.",
            "critical",
            "warning");

        AddThresholdAlert(
            alerts,
            "notification-deliveries-oldest-age",
            oldestQueuedAgeMinutes,
            _options.OldestQueuedWarningMinutes,
            _options.OldestQueuedCriticalMinutes,
            "Notification delivery co tuoi hang doi qua nguong critical.",
            "Notification delivery co tuoi hang doi qua nguong warning.",
            "critical",
            "warning",
            "minutes");
    }

    private void EvaluateBackgroundWorkers(ICollection<OperationalAlertItem> alerts, DateTime generatedAtUtc)
    {
        foreach (var snapshot in _backgroundWorkerHealthRegistry.GetAll())
        {
            if (string.Equals(snapshot.Status, "Unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                alerts.Add(new OperationalAlertItem
                {
                    Code = $"worker-unhealthy:{snapshot.WorkerName}",
                    Severity = "critical",
                    Message = $"Worker {snapshot.WorkerName} dang o trang thai unhealthy.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["worker"] = snapshot.WorkerName,
                        ["detail"] = snapshot.Detail,
                        ["lastErrorUtc"] = snapshot.LastErrorUtc
                    }
                });

                continue;
            }

            var silenceMinutes = Math.Max(0, Math.Round((generatedAtUtc - snapshot.LastHeartbeatUtc).TotalMinutes, 2));
            if (silenceMinutes >= _options.WorkerStaleCriticalMinutes)
            {
                alerts.Add(new OperationalAlertItem
                {
                    Code = $"worker-stale:{snapshot.WorkerName}",
                    Severity = "critical",
                    Message = $"Worker {snapshot.WorkerName} khong heartbeat trong thoi gian dai.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["worker"] = snapshot.WorkerName,
                        ["silenceMinutes"] = silenceMinutes,
                        ["status"] = snapshot.Status
                    }
                });
            }
            else if (silenceMinutes >= _options.WorkerStaleWarningMinutes)
            {
                alerts.Add(new OperationalAlertItem
                {
                    Code = $"worker-stale:{snapshot.WorkerName}",
                    Severity = "warning",
                    Message = $"Worker {snapshot.WorkerName} heartbeat cham hon nguong mong doi.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["worker"] = snapshot.WorkerName,
                        ["silenceMinutes"] = silenceMinutes,
                        ["status"] = snapshot.Status
                    }
                });
            }
        }
    }

    private void EvaluateDashboardCache(ICollection<OperationalAlertItem> alerts)
    {
        foreach (var snapshot in _dashboardCacheMetricsRegistry.GetSnapshots())
        {
            var totalReads = snapshot.Hits + snapshot.Misses;
            if (totalReads < _options.CacheMinimumSamples)
            {
                continue;
            }

            var hitRatioPercent = (double)snapshot.Hits / totalReads * 100;
            if (hitRatioPercent <= _options.CacheHitRatioCriticalPercent)
            {
                alerts.Add(new OperationalAlertItem
                {
                    Code = $"dashboard-cache-hit-ratio:{snapshot.CacheSegment}",
                    Severity = "critical",
                    Message = $"Dashboard cache hit ratio cua segment {snapshot.CacheSegment} dang thap.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["segment"] = snapshot.CacheSegment,
                        ["hitRatioPercent"] = Math.Round(hitRatioPercent, 2),
                        ["hits"] = snapshot.Hits,
                        ["misses"] = snapshot.Misses
                    }
                });
            }
            else if (hitRatioPercent <= _options.CacheHitRatioWarningPercent)
            {
                alerts.Add(new OperationalAlertItem
                {
                    Code = $"dashboard-cache-hit-ratio:{snapshot.CacheSegment}",
                    Severity = "warning",
                    Message = $"Dashboard cache hit ratio cua segment {snapshot.CacheSegment} dang duoi nguong warning.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["segment"] = snapshot.CacheSegment,
                        ["hitRatioPercent"] = Math.Round(hitRatioPercent, 2),
                        ["hits"] = snapshot.Hits,
                        ["misses"] = snapshot.Misses
                    }
                });
            }
        }
    }

    private static void AddThresholdAlert(
        ICollection<OperationalAlertItem> alerts,
        string code,
        double actualValue,
        double warningThreshold,
        double criticalThreshold,
        string criticalMessage,
        string warningMessage,
        string criticalSeverity,
        string warningSeverity,
        string unit = "count")
    {
        if (actualValue >= criticalThreshold)
        {
            alerts.Add(new OperationalAlertItem
            {
                Code = code,
                Severity = criticalSeverity,
                Message = criticalMessage,
                Metadata = new Dictionary<string, object?>
                {
                    ["actual"] = actualValue,
                    ["unit"] = unit,
                    ["threshold"] = criticalThreshold
                }
            });
        }
        else if (actualValue >= warningThreshold)
        {
            alerts.Add(new OperationalAlertItem
            {
                Code = code,
                Severity = warningSeverity,
                Message = warningMessage,
                Metadata = new Dictionary<string, object?>
                {
                    ["actual"] = actualValue,
                    ["unit"] = unit,
                    ["threshold"] = warningThreshold
                }
            });
        }
    }

    private static string ResolveOverallStatus(IEnumerable<OperationalAlertItem> alerts)
    {
        if (alerts.Any(x => string.Equals(x.Severity, "critical", StringComparison.OrdinalIgnoreCase)))
        {
            return "critical";
        }

        if (alerts.Any(x => string.Equals(x.Severity, "warning", StringComparison.OrdinalIgnoreCase)))
        {
            return "warning";
        }

        return "healthy";
    }
}

public class OperationalAlertSnapshot
{
    public DateTime GeneratedAtUtc { get; set; }
    public string Status { get; set; } = "healthy";
    public IReadOnlyCollection<OperationalAlertItem> Alerts { get; set; } = Array.Empty<OperationalAlertItem>();
}

public class OperationalAlertItem
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
}
