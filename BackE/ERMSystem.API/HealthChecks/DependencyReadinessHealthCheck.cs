using ERMSystem.Infrastructure.Messaging;
using ERMSystem.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ERMSystem.API.HealthChecks;

public sealed class DependencyReadinessHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DependencyReadinessHealthCheck> _logger;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;
    private readonly DistributedCacheRuntimeInfo _distributedCacheRuntimeInfo;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly IDistributedCache _distributedCache;

    public DependencyReadinessHealthCheck(
        IConfiguration configuration,
        ILogger<DependencyReadinessHealthCheck> logger,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        DistributedCacheRuntimeInfo distributedCacheRuntimeInfo,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IDistributedCache distributedCache)
    {
        _configuration = configuration;
        _logger = logger;
        _workerHealthRegistry = workerHealthRegistry;
        _distributedCacheRuntimeInfo = distributedCacheRuntimeInfo;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _distributedCache = distributedCache;
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
            await CheckDistributedCacheAsync(data, failures, cancellationToken);
        }
        else
        {
            data["redis"] = "skipped";
        }

        if (_rabbitMqOptions.Enabled)
        {
            await CheckRabbitMqAsync(data, failures, cancellationToken);
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

    private async Task CheckDistributedCacheAsync(
        IDictionary<string, object> data,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        var key = $"health:readiness:{Guid.NewGuid():N}";
        var expectedValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        try
        {
            await _distributedCache.SetStringAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);

            var actualValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            await _distributedCache.RemoveAsync(key, cancellationToken);

            if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
            {
                data["redis"] = "roundtrip-mismatch";
                failures.Add("redis cache roundtrip mismatch");
                return;
            }

            data["redis"] = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency check failed for Redis distributed cache.");
            data["redis"] = ex.GetType().Name;
            failures.Add("redis cache roundtrip failed");
        }
    }

    private async Task CheckRabbitMqAsync(
        IDictionary<string, object> data,
        ICollection<string> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            await Task.Run(() =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMqOptions.Host,
                    Port = _rabbitMqOptions.Port,
                    UserName = _rabbitMqOptions.Username,
                    Password = _rabbitMqOptions.Password,
                    VirtualHost = _rabbitMqOptions.VirtualHost,
                    AutomaticRecoveryEnabled = false,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
                };

                using var connection = factory.CreateConnection("ermsystem-readiness");
                using var channel = connection.CreateModel();
            }, timeoutCts.Token);

            data["rabbitMq"] = "ok";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dependency check failed for RabbitMQ.");
            data["rabbitMq"] = ex.GetType().Name;
            failures.Add("rabbitMq broker check failed");
        }
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
