using System.Collections.Concurrent;
using ERMSystem.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERMSystem.API.Services;

public class OperationalAlertDispatchService : BackgroundService
{
    private readonly OperationalAlertEvaluator _evaluator;
    private readonly OperationalAlertWebhookNotifier _webhookNotifier;
    private readonly OperationalAlertOptions _options;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;
    private readonly ILogger<OperationalAlertDispatchService> _logger;
    private readonly ConcurrentDictionary<string, OperationalAlertDispatchState> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);

    public OperationalAlertDispatchService(
        OperationalAlertEvaluator evaluator,
        OperationalAlertWebhookNotifier webhookNotifier,
        IOptions<OperationalAlertOptions> options,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        ILogger<OperationalAlertDispatchService> logger)
    {
        _evaluator = evaluator;
        _webhookNotifier = webhookNotifier;
        _options = options.Value;
        _workerHealthRegistry = workerHealthRegistry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.DispatchEnabled)
        {
            _logger.LogInformation("Operational alert dispatch dang tat trong cau hinh.");
            _workerHealthRegistry.Report("operational-alert-dispatch", "Disabled", "Dispatch disabled by configuration.");
            return;
        }

        _logger.LogInformation("Khoi dong worker operational alert dispatch.");
        _workerHealthRegistry.Report("operational-alert-dispatch", "Starting", "Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(15, _options.DispatchIntervalSeconds)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAlertsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker operational alert dispatch gap loi khong mong muon.");
                _workerHealthRegistry.Report("operational-alert-dispatch", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DispatchAlertsAsync(CancellationToken ct)
    {
        var snapshot = await _evaluator.EvaluateAsync(ct);
        var nowUtc = snapshot.GeneratedAtUtc;
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alert in snapshot.Alerts)
        {
            seenCodes.Add(alert.Code);

            if (!_activeAlerts.TryGetValue(alert.Code, out var state))
            {
                await SendAsync("activated", snapshot.Status, alert, ct);
                _activeAlerts[alert.Code] = new OperationalAlertDispatchState
                {
                    Severity = alert.Severity,
                    FirstSeenAtUtc = nowUtc,
                    LastSentAtUtc = nowUtc,
                    LastAlert = Clone(alert)
                };
                continue;
            }

            var shouldRepeat = nowUtc - state.LastSentAtUtc >= TimeSpan.FromMinutes(Math.Max(1, _options.RepeatIntervalMinutes));
            var severityChanged = !string.Equals(state.Severity, alert.Severity, StringComparison.OrdinalIgnoreCase);

            if (severityChanged || shouldRepeat)
            {
                await SendAsync(severityChanged ? "escalated" : "repeated", snapshot.Status, alert, ct);
                state.Severity = alert.Severity;
                state.LastSentAtUtc = nowUtc;
                state.LastAlert = Clone(alert);
            }
            else
            {
                state.LastAlert = Clone(alert);
                state.Severity = alert.Severity;
            }
        }

        foreach (var item in _activeAlerts.ToArray())
        {
            if (seenCodes.Contains(item.Key))
            {
                continue;
            }

            if (_options.SendResolvedAlerts)
            {
                await SendAsync("resolved", "healthy", item.Value.LastAlert, ct);
            }

            _activeAlerts.TryRemove(item.Key, out _);
        }

        _workerHealthRegistry.Report(
            "operational-alert-dispatch",
            "Healthy",
            $"Evaluated alert snapshot status={snapshot.Status}, activeAlerts={snapshot.Alerts.Count}.",
            successAtUtc: nowUtc);
    }

    private async Task SendAsync(string eventType, string overallStatus, OperationalAlertItem alert, CancellationToken ct)
    {
        if (!_webhookNotifier.IsEnabled())
        {
            _logger.LogWarning(
                "Operational alert dispatch dang bat nhung webhook chua cau hinh day du. EventType={EventType}, Code={Code}",
                eventType,
                alert.Code);
            return;
        }

        await _webhookNotifier.SendAsync(new OperationalAlertNotificationEnvelope
        {
            GeneratedAtUtc = DateTime.UtcNow,
            EventType = eventType,
            OverallStatus = overallStatus,
            Alert = Clone(alert)
        }, ct);
    }

    private static OperationalAlertItem Clone(OperationalAlertItem alert)
        => new()
        {
            Code = alert.Code,
            Severity = alert.Severity,
            Message = alert.Message,
            Metadata = new Dictionary<string, object?>(alert.Metadata, StringComparer.OrdinalIgnoreCase)
        };
}
