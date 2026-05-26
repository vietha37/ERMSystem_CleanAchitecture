using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalEncounterRepository
{
    Task<PaginatedResult<HospitalEncounterSummaryDto>> GetWorklistAsync(
        HospitalEncounterWorklistRequestDto request,
        CancellationToken ct = default);

    Task<HospitalEncounterAggregateSnapshot?> GetEncounterAggregateAsync(
        Guid encounterId,
        CancellationToken ct = default);
    Task<HospitalEncounterAttachmentSnapshot?> GetAttachmentAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default);
    Task<string[]> GetReferencedAttachmentUrisAsync(CancellationToken ct = default);

    Task<HospitalEncounterAppointmentSnapshot?> GetAppointmentForEncounterAsync(
        Guid appointmentId,
        CancellationToken ct = default);

    Task<HospitalEncounterEligibleAppointmentDto[]> GetEligibleAppointmentsAsync(CancellationToken ct = default);
    Task<bool> HospitalUserExistsAsync(Guid userId, CancellationToken ct = default);

    Task AddEncounterAsync(HospitalEncounterCreateCommand command, CancellationToken ct = default);
    Task AddVitalSignAsync(HospitalEncounterVitalSignCreateCommand command, CancellationToken ct = default);
    Task AddDiagnosisAsync(HospitalEncounterDiagnosisCreateCommand command, CancellationToken ct = default);
    Task AddClinicalNoteAsync(HospitalEncounterClinicalNoteCreateCommand command, CancellationToken ct = default);
    Task UpdateEncounterAsync(HospitalEncounterUpdateCommand command, CancellationToken ct = default);
    Task UpdateVitalSignAsync(HospitalEncounterVitalSignUpdateCommand command, CancellationToken ct = default);
    Task UpdateDiagnosisAsync(HospitalEncounterDiagnosisUpdateCommand command, CancellationToken ct = default);
    Task UpdateClinicalNoteAsync(HospitalEncounterClinicalNoteUpdateCommand command, CancellationToken ct = default);
    Task AddAttachmentAsync(HospitalEncounterAttachmentCreateCommand command, CancellationToken ct = default);
    Task AddOutboxMessageAsync(HospitalEncounterOutboxCreateCommand command, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class HospitalEncounterAggregateSnapshot
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public string EncounterType { get; set; } = string.Empty;
    public string EncounterStatus { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public string? AppointmentNumber { get; set; }
    public DateTime? AppointmentStartUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? Summary { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? VitalSignId { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public Guid? DiagnosisId { get; set; }
    public string? DiagnosisType { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? DiagnosisName { get; set; }
    public Guid? ClinicalNoteId { get; set; }
    public DateTime? ClinicalNoteAuthoredAtUtc { get; set; }
    public DateTime? ClinicalNoteSignedAtUtc { get; set; }
    public string? ClinicalNoteSignedByUsername { get; set; }
    public Guid? ApprovalNoteId { get; set; }
    public DateTime? ApprovalSignedAtUtc { get; set; }
    public string? ApprovedByUsername { get; set; }
    public string? ApprovalComment { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? CarePlan { get; set; }
    public HospitalEncounterWorkflowEventSnapshot[] WorkflowEvents { get; set; } = Array.Empty<HospitalEncounterWorkflowEventSnapshot>();
    public HospitalEncounterAttachmentSnapshot[] Attachments { get; set; } = Array.Empty<HospitalEncounterAttachmentSnapshot>();
}

public class HospitalEncounterWorkflowEventSnapshot
{
    public string EventType { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? PerformedByUsername { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public class HospitalEncounterAttachmentSnapshot
{
    public Guid AttachmentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string DocumentUri { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string? UploadedByUsername { get; set; }
}

public class HospitalEncounterAppointmentSnapshot
{
    public Guid AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public string AppointmentStatus { get; set; } = string.Empty;
    public DateTime AppointmentStartUtc { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public Guid? ExistingEncounterId { get; set; }
    public string? ExistingEncounterNumber { get; set; }
}

public class HospitalEncounterCreateCommand
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid DoctorProfileId { get; set; }
    public Guid ClinicId { get; set; }
    public string EncounterType { get; set; } = string.Empty;
    public string EncounterStatus { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class HospitalEncounterVitalSignCreateCommand
{
    public Guid VitalSignId { get; set; }
    public Guid EncounterId { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByUserId { get; set; }
}

public class HospitalEncounterDiagnosisCreateCommand
{
    public Guid DiagnosisId { get; set; }
    public Guid EncounterId { get; set; }
    public string DiagnosisType { get; set; } = string.Empty;
    public string? DiagnosisCode { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime NotedAtUtc { get; set; }
}

public class HospitalEncounterClinicalNoteCreateCommand
{
    public Guid ClinicalNoteId { get; set; }
    public Guid EncounterId { get; set; }
    public string NoteType { get; set; } = string.Empty;
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? CarePlan { get; set; }
    public Guid? AuthoredByUserId { get; set; }
    public DateTime AuthoredAtUtc { get; set; }
    public DateTime? SignedAtUtc { get; set; }
}

public class HospitalEncounterUpdateCommand
{
    public Guid EncounterId { get; set; }
    public string EncounterStatus { get; set; } = string.Empty;
    public DateTime? EndedAtUtc { get; set; }
    public string? Summary { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class HospitalEncounterVitalSignUpdateCommand
{
    public Guid VitalSignId { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByUserId { get; set; }
}

public class HospitalEncounterDiagnosisUpdateCommand
{
    public Guid DiagnosisId { get; set; }
    public string DiagnosisType { get; set; } = string.Empty;
    public string? DiagnosisCode { get; set; }
    public string DiagnosisName { get; set; } = string.Empty;
    public DateTime NotedAtUtc { get; set; }
}

public class HospitalEncounterClinicalNoteUpdateCommand
{
    public Guid ClinicalNoteId { get; set; }
    public string? NoteType { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? CarePlan { get; set; }
    public Guid? AuthoredByUserId { get; set; }
    public DateTime AuthoredAtUtc { get; set; }
    public DateTime? SignedAtUtc { get; set; }
}

public class HospitalEncounterAttachmentCreateCommand
{
    public Guid AttachmentId { get; set; }
    public Guid EncounterId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string DocumentUri { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public Guid? UploadedByUserId { get; set; }
}

public class HospitalEncounterOutboxCreateCommand
{
    public Guid OutboxMessageId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableAtUtc { get; set; }
}
