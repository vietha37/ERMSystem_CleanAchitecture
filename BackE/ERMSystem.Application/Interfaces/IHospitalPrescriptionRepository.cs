using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalPrescriptionRepository
{
    Task<PaginatedResult<HospitalPrescriptionSummaryDto>> GetWorklistAsync(
        HospitalPrescriptionWorklistRequestDto request,
        CancellationToken ct = default);

    Task<HospitalPrescriptionAggregateSnapshot?> GetByIdAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<HospitalPrescriptionEligibleEncounterDto[]> GetEligibleEncountersAsync(Guid? doctorProfileId, CancellationToken ct = default);
    Task<HospitalMedicineCatalogDto[]> GetMedicineCatalogAsync(CancellationToken ct = default);
    Task<HospitalPrescriptionEncounterSnapshot?> GetEncounterForPrescriptionAsync(Guid encounterId, CancellationToken ct = default);
    Task<HospitalMedicineSnapshot[]> GetMedicinesByIdsAsync(Guid[] medicineIds, CancellationToken ct = default);
    Task<bool> HospitalUserExistsAsync(Guid userId, CancellationToken ct = default);
    Task AddOrderHeaderAsync(HospitalPrescriptionOrderHeaderCreateCommand command, CancellationToken ct = default);
    Task AddPrescriptionAsync(HospitalPrescriptionCreateCommand command, CancellationToken ct = default);
    Task AddPrescriptionItemAsync(HospitalPrescriptionItemCreateCommand command, CancellationToken ct = default);
    Task AddDispensingAsync(HospitalPrescriptionDispensingCreateCommand command, CancellationToken ct = default);
    Task UpdatePrescriptionStatusAsync(Guid prescriptionId, string status, CancellationToken ct = default);
    Task UpdateOrderHeaderStatusAsync(Guid orderHeaderId, string status, CancellationToken ct = default);
    Task AddOutboxMessageAsync(HospitalPrescriptionOutboxCreateCommand command, CancellationToken ct = default);
    Task DeletePrescriptionAsync(Guid prescriptionId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class HospitalPrescriptionAggregateSnapshot
{
    public Guid PrescriptionId { get; set; }
    public Guid OrderHeaderId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? LatestDispensingId { get; set; }
    public string? LatestDispensingStatus { get; set; }
    public DateTime? DispensedAtUtc { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public string? DispensedByUsername { get; set; }
    public string? DispensingNotes { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public DateOnly PatientDateOfBirth { get; set; }
    public string PatientGender { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string? PrimaryDiagnosisName { get; set; }
    public string[] DiagnosisNames { get; set; } = Array.Empty<string>();
    public DateTime CreatedAtUtc { get; set; }
    public string? Notes { get; set; }
    public HospitalPrescriptionLabResultSnapshot[] LabResults { get; set; } = Array.Empty<HospitalPrescriptionLabResultSnapshot>();
    public HospitalPrescriptionDispensingSnapshot[] DispensingHistory { get; set; } = Array.Empty<HospitalPrescriptionDispensingSnapshot>();
    public HospitalPrescriptionItemSnapshot[] Items { get; set; } = Array.Empty<HospitalPrescriptionItemSnapshot>();
}

public class HospitalPrescriptionLabResultSnapshot
{
    public string? AnalyteCode { get; set; }
    public string AnalyteName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public class HospitalPrescriptionDispensingSnapshot
{
    public Guid DispensingId { get; set; }
    public string DispensingStatus { get; set; } = string.Empty;
    public DateTime? DispensedAtUtc { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public string? DispensedByUsername { get; set; }
    public string? Notes { get; set; }
}

public class HospitalPrescriptionItemSnapshot
{
    public Guid PrescriptionItemId { get; set; }
    public Guid MedicineId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Unit { get; set; }
    public string DoseInstruction { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public int? DurationDays { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class HospitalPrescriptionEncounterSnapshot
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public string EncounterStatus { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string? PrimaryDiagnosisName { get; set; }
    public Guid? ExistingPrescriptionId { get; set; }
    public string? ExistingPrescriptionNumber { get; set; }
}

public class HospitalMedicineSnapshot
{
    public Guid MedicineId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string? Unit { get; set; }
    public bool IsControlled { get; set; }
}

public class HospitalPrescriptionOrderHeaderCreateCommand
{
    public Guid OrderHeaderId { get; set; }
    public Guid EncounterId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderCategory { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public Guid? OrderedByUserId { get; set; }
    public DateTime OrderedAtUtc { get; set; }
}

public class HospitalPrescriptionCreateCommand
{
    public Guid PrescriptionId { get; set; }
    public Guid OrderHeaderId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class HospitalPrescriptionItemCreateCommand
{
    public Guid PrescriptionItemId { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid MedicineId { get; set; }
    public string DoseInstruction { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public int? DurationDays { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class HospitalPrescriptionOutboxCreateCommand
{
    public Guid OutboxMessageId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableAtUtc { get; set; }
}

public class HospitalPrescriptionDispensingCreateCommand
{
    public Guid DispensingId { get; set; }
    public Guid PrescriptionId { get; set; }
    public string DispensingStatus { get; set; } = string.Empty;
    public DateTime DispensedAtUtc { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public string? Notes { get; set; }
}
