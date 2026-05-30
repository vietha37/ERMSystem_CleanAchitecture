using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs;

public class HospitalMedicineCatalogDto
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

public class HospitalPrescriptionWorklistRequestDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [MaxLength(30)]
    public string? Status { get; set; }

    [MaxLength(200)]
    public string? TextSearch { get; set; }

    public Guid? DoctorProfileId { get; set; }
}

public class HospitalPrescriptionSummaryDto
{
    public Guid PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LatestDispensingStatus { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string? PrimaryDiagnosisName { get; set; }
    public int TotalItems { get; set; }
    public DateTime CreatedAtLocal { get; set; }
    public DateTime? DispensedAtLocal { get; set; }
    public string? DispensedByUsername { get; set; }
    public string? Notes { get; set; }
}

public class HospitalPrescriptionItemDto
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

public class HospitalPrescriptionDetailDto : HospitalPrescriptionSummaryDto
{
    public Guid? LatestDispensingId { get; set; }
    public string? DispensingNotes { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<HospitalPrescriptionWarningDto> WarningDetails { get; set; } = new();
    public List<HospitalPrescriptionDispensingHistoryDto> DispensingHistory { get; set; } = new();
    public List<HospitalPrescriptionItemDto> Items { get; set; } = new();
}

public class HospitalPrescriptionWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public List<string> RelatedMedicines { get; set; } = new();
}

public class HospitalPrescriptionDispensingHistoryDto
{
    public Guid DispensingId { get; set; }
    public string DispensingStatus { get; set; } = string.Empty;
    public DateTime? DispensedAtLocal { get; set; }
    public Guid? DispensedByUserId { get; set; }
    public string? DispensedByUsername { get; set; }
    public string? Notes { get; set; }
}

public class HospitalPrescriptionEligibleEncounterDto
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string EncounterStatus { get; set; } = string.Empty;
    public string? PrimaryDiagnosisName { get; set; }
    public DateTime StartedAtLocal { get; set; }
    public Guid? ExistingPrescriptionId { get; set; }
    public string? ExistingPrescriptionNumber { get; set; }
}

public class CreateHospitalPrescriptionItemDto
{
    [Required]
    public Guid MedicineId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DoseInstruction { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Route { get; set; }

    [MaxLength(100)]
    public string? Frequency { get; set; }

    [Range(1, 365)]
    public int? DurationDays { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Quantity { get; set; }
}

public class CreateHospitalPrescriptionDto
{
    [Required]
    public Guid EncounterId { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Issued";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MinLength(1)]
    public List<CreateHospitalPrescriptionItemDto> Items { get; set; } = new();
}

public class DispenseHospitalPrescriptionDto
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
