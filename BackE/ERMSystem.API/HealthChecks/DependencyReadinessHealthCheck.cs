using System.Net.Sockets;
using ERMSystem.Infrastructure.Messaging;
using ERMSystem.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ERMSystem.API.HealthChecks;

public sealed class DependencyReadinessHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DependencyReadinessHealthCheck> _logger;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;
    private readonly DistributedCacheRuntimeInfo _distributedCacheRuntimeInfo;
    private readonly RabbitMqOptions _rabbitMqOptions;

    public DependencyReadinessHealthCheck(
        IConfiguration configuration,
        ILogger<DependencyReadinessHealthCheck> logger,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        DistributedCacheRuntimeInfo distributedCacheRuntimeInfo,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _configuration = configuration;
        _logger = logger;
        _workerHealthRegistry = workerHealthRegistry;
        _distributedCacheRuntimeInfo = distributedCacheRuntimeInfo;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var failures = new List<string>();

        await CheckSqlAsync(
            "hospitalDb",
            _configuration.GetConnectionString("HospitalConnection"),
            data,
            failures,
            cancellationToken);

        data["distributedCache"] = new
        {
            _distributedCacheRuntimeInfo.Provider,
            _distributedCacheRuntimeInfo.IsFallback,
            _distributedCacheRuntimeInfo.Reason
        };

        if (string.Equals(_distributedCacheRuntimeInfo.Provider, "redis", StringComparison.OrdinalIgnoreCase))
        {
            await CheckTcpAsync("redis", _configuration["Redis:ConnectionString"], 6379, data, failures, cancellationToken);
        }
        else
        {
            data["redis"] = "skipped";
        }

        if (_rabbitMqOptions.Enabled)
        {
            await CheckTcpAsync(
                "rabbitMq",
                $"{_rabbitMqOptions.Host}:{_rabbitMqOptions.Port}",
                5672,
                data,
                failures,
                cancellationToken);
        }
        else
        {
            data["rabbitMq"] = "skipped";
        }

        CheckWorkers(data, failures);

        return failures.Count == 0
            ? HealthCheckResult.Healthy("All dependencies are reachable.", data: data)
            : HealthCheckResult.Unhealthy(
                $"Dependency readiness failed: {string.Join("; ", failures)}",
                data: data);
    }

    private async Task CheckSqlAsync(
        string name,
        string? connectionString,
        IDictionary<string, object> data,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            data[name] = "missing-connection-string";
            failures.Add($"{name} missing connection string");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            await command.ExecuteScalarAsync(cancellationToken);
            data[name] = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency check failed for {Dependency}.", name);
            data[name] = ex.GetType().Name;
            failures.Add($"{name} unreachable");
        }
    }

    private async Task CheckTcpAsync(
        string name,
        string? endpointValue,
        int defaultPort,
        IDictionary<string, object> data,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        if (!TryParseEndpoint(endpointValue, defaultPort, out var host, out var port))
        {
            data[name] = "missing-endpoint";
            failures.Add($"{name} missing endpoint");
            return;
        }

        try
        {
            using var tcpClient = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
            await tcpClient.ConnectAsync(host, port, timeoutCts.Token);
            data[name] = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency check failed for {Dependency}.", name);
            data[name] = ex.GetType().Name;
            failures.Add($"{name} unreachable");
        }
    }

    private static bool TryParseEndpoint(
        string? endpointValue,
        int defaultPort,
        out string host,
        out int port)
    {
        host = string.Empty;
        port = defaultPort;

        if (string.IsNullOrWhiteSpace(endpointValue))
        {
            return false;
        }

        var firstEndpoint = endpointValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstEndpoint))
        {
            return false;
        }

        var parts = firstEndpoint.Split(':', StringSplitOptions.TrimEntries);
        host = parts[0];
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (parts.Length >= 2 && int.TryParse(parts[^1], out var parsedPort))
        {
            port = parsedPort;
        }

        return true;
    }

    private void CheckWorkers(
        IDictionary<string, object> data,
        ICollection<string> failures)
    {
        var workerSnapshots = _workerHealthRegistry.GetAll();
        var nowUtc = DateTime.UtcNow;
        var requiredWorkers = new List<string>
        {
            "hospital-notification-dispatch",
            "retention-cleanup",
            "revisit-reminder-campaign",
            "satisfaction-survey-campaign",
            "customer-care-follow-up-campaign"
        };

        if (_rabbitMqOptions.Enabled)
        {
            requiredWorkers.Insert(0, "hospital-notification-consumer");
            requiredWorkers.Insert(0, "hospital-outbox-publisher");
        }

        var workerData = new Dictionary<string, object>();
        foreach (var workerName in requiredWorkers)
        {
            var snapshot = workerSnapshots.FirstOrDefault(x => x.WorkerName == workerName);
            if (snapshot == null)
            {
                workerData[workerName] = "missing";
                failures.Add($"{workerName} missing");
                continue;
            }

            var silence = nowUtc - snapshot.LastHeartbeatUtc;
            workerData[workerName] = new
            {
                snapshot.Status,
                snapshot.Detail,
                snapshot.LastHeartbeatUtc,
                snapshot.LastSuccessUtc,
                snapshot.LastErrorUtc,
                SilenceSeconds = Math.Round(silence.TotalSeconds, 2)
            };

            if (string.Equals(snapshot.Status, "Unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{workerName} unhealthy");
                continue;
            }

            if (silence > TimeSpan.FromMinutes(10))
            {
                failures.Add($"{workerName} stale");
            }
        }

        data["workers"] = workerData;
    }
}
