using System.Collections.Concurrent;

namespace ERMSystem.Infrastructure.Services;

public class BackgroundWorkerHealthRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundWorkerHealthSnapshot> _workers = new(StringComparer.Ordinal);

    public void Report(
        string workerName,
        string status,
        string? detail = null,
        DateTime? successAtUtc = null,
        DateTime? errorAtUtc = null)
    {
        var nowUtc = DateTime.UtcNow;
        _workers.AddOrUpdate(
            workerName,
            _ => new BackgroundWorkerHealthSnapshot
            {
                WorkerName = workerName,
                Status = status,
                Detail = detail,
                LastHeartbeatUtc = nowUtc,
                LastSuccessUtc = successAtUtc,
                LastErrorUtc = errorAtUtc
            },
            (_, current) =>
            {
                current.Status = status;
                current.Detail = detail;
                current.LastHeartbeatUtc = nowUtc;
                current.LastSuccessUtc = successAtUtc ?? current.LastSuccessUtc;
                current.LastErrorUtc = errorAtUtc ?? current.LastErrorUtc;
                return current;
            });
    }

    public IReadOnlyCollection<BackgroundWorkerHealthSnapshot> GetAll()
        => _workers.Values
            .OrderBy(x => x.WorkerName, StringComparer.Ordinal)
            .ToArray();
}

public class BackgroundWorkerHealthSnapshot
{
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public string? Detail { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public DateTime? LastSuccessUtc { get; set; }
    public DateTime? LastErrorUtc { get; set; }
}
