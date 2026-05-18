namespace ERMSystem.Application.Interfaces;

public interface IComplianceAuditRecorder
{
    Task RecordAsync(
        Guid? userId,
        string username,
        string eventType,
        string severity,
        string detail,
        CancellationToken ct = default);
}
