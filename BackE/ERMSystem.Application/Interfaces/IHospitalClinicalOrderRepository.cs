using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalClinicalOrderRepository
{
    Task<PaginatedResult<HospitalClinicalOrderSummaryDto>> GetWorklistAsync(
        HospitalClinicalOrderWorklistRequestDto request,
        CancellationToken ct = default);

    Task<HospitalClinicalOrderDetailSnapshot?> GetByIdAsync(Guid clinicalOrderId, CancellationToken ct = default);
    Task<HospitalClinicalOrderEligibleEncounterDto[]> GetEligibleEncountersAsync(Guid? doctorProfileId, CancellationToken ct = default);
    Task<HospitalClinicalOrderCatalogItemDto[]> GetCatalogAsync(CancellationToken ct = default);
    Task<HospitalClinicalOrderEncounterSnapshot?> GetEncounterForOrderingAsync(Guid encounterId, CancellationToken ct = default);
    Task<HospitalClinicalOrderServiceSnapshot?> GetCatalogServiceAsync(string category, Guid serviceId, CancellationToken ct = default);
    Task AddOrderHeaderAsync(HospitalClinicalOrderHeaderCreateCommand command, CancellationToken ct = default);
    Task AddLabOrderAsync(HospitalLabOrderCreateCommand command, CancellationToken ct = default);
    Task AddImagingOrderAsync(HospitalImagingOrderCreateCommand command, CancellationToken ct = default);
    Task AddSpecimenAsync(HospitalSpecimenCreateCommand command, CancellationToken ct = default);
    Task ReplaceLabResultItemsAsync(Guid labOrderId, IReadOnlyCollection<HospitalLabResultItemCreateCommand> items, CancellationToken ct = default);
    Task UpdateLabOrderCompletionAsync(Guid labOrderId, string status, DateTime resultedAtUtc, CancellationToken ct = default);
    Task AddImagingReportAsync(HospitalImagingReportCreateCommand command, CancellationToken ct = default);
    Task UpdateImagingOrderCompletionAsync(Guid imagingOrderId, string status, DateTime reportedAtUtc, CancellationToken ct = default);
    Task UpdateOrderHeaderStatusAsync(Guid orderHeaderId, string status, CancellationToken ct = default);
    Task AddOutboxMessageAsync(HospitalClinicalOrderOutboxCreateCommand command, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class HospitalClinicalOrderDetailSnapshot
{
    public Guid ClinicalOrderId { get; set; }
    public Guid OrderHeaderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? PriorityCode { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? OrderedByUsername { get; set; }
    public Guid? SpecimenId { get; set; }
    public string? SpecimenCode { get; set; }
    public string? SpecimenStatus { get; set; }
    public DateTime? CollectedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? ReportUri { get; set; }
    public string? SignedByUsername { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public HospitalClinicalOrderLabResultItemSnapshot[] ResultItems { get; set; } = Array.Empty<HospitalClinicalOrderLabResultItemSnapshot>();
}

public class HospitalClinicalOrderLabResultItemSnapshot
{
    public Guid ResultItemId { get; set; }
    public string? AnalyteCode { get; set; }
    public string AnalyteName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
}

public class HospitalClinicalOrderEncounterSnapshot
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public string EncounterStatus { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
}

public class HospitalClinicalOrderServiceSnapshot
{
    public Guid ServiceId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}

public class HospitalClinicalOrderHeaderCreateCommand
{
    public Guid OrderHeaderId { get; set; }
    public Guid EncounterId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderCategory { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public Guid? OrderedByUserId { get; set; }
    public DateTime OrderedAtUtc { get; set; }
}

public class HospitalLabOrderCreateCommand
{
    public Guid LabOrderId { get; set; }
    public Guid OrderHeaderId { get; set; }
    public Guid LabServiceId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string? PriorityCode { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}

public class HospitalImagingOrderCreateCommand
{
    public Guid ImagingOrderId { get; set; }
    public Guid OrderHeaderId { get; set; }
    public Guid ImagingServiceId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
}

public class HospitalSpecimenCreateCommand
{
    public Guid SpecimenId { get; set; }
    public Guid LabOrderId { get; set; }
    public string SpecimenCode { get; set; } = string.Empty;
    public DateTime? CollectedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class HospitalLabResultItemCreateCommand
{
    public Guid ResultItemId { get; set; }
    public Guid LabOrderId { get; set; }
    public string? AnalyteCode { get; set; }
    public string AnalyteName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public class HospitalImagingReportCreateCommand
{
    public Guid ImagingReportId { get; set; }
    public Guid ImagingOrderId { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? ReportUri { get; set; }
    public Guid? SignedByUserId { get; set; }
    public DateTime? SignedAtUtc { get; set; }
}

public class HospitalClinicalOrderOutboxCreateCommand
{
    public Guid OutboxMessageId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableAtUtc { get; set; }
}
