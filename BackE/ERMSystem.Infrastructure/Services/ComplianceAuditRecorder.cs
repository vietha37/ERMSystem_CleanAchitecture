using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;

namespace ERMSystem.Infrastructure.Services;

public class ComplianceAuditRecorder : IComplianceAuditRecorder
{
    private readonly HospitalDbContext _hospitalDbContext;

    public ComplianceAuditRecorder(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task RecordAsync(
        Guid? userId,
        string username,
        string eventType,
        string severity,
        string detail,
        CancellationToken ct = default)
    {
        try
        {
            _hospitalDbContext.SecurityEvents.Add(new HospitalSecurityEventEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = eventType,
                Severity = severity,
                Detail = SensitiveDataMasking.MaskAuditDetail($"Username={username}; {detail}"),
                IpAddress = null,
                UserAgent = null,
                OccurredAtUtc = DateTime.UtcNow
            });

            await _hospitalDbContext.SaveChangesAsync(ct);
        }
        catch
        {
            // Compliance audit should not break the main business flow.
        }
    }
}
