using System.ComponentModel.DataAnnotations;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.DTOs;

public class HospitalClinicalOrderWorklistRequestDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [MaxLength(20)]
    public string? Category { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }

    [MaxLength(200)]
    public string? TextSearch { get; set; }

    public Guid? DoctorProfileId { get; set; }
}

public class HospitalClinicalOrderCatalogItemDto
{
    public Guid ServiceId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? ExtraLabel { get; set; }
}

public class HospitalClinicalOrderEligibleEncounterDto
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
}

public class HospitalClinicalOrderSummaryDto
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
    public DateTime RequestedAtLocal { get; set; }
    public DateTime? CompletedAtLocal { get; set; }
    public string? OrderedByUsername { get; set; }
    public int ResultItemCount { get; set; }
    public string? SummaryText { get; set; }
}

public class HospitalLabResultItemDto
{
    public Guid ResultItemId { get; set; }
    public string? AnalyteCode { get; set; }
    public string AnalyteName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
}

public class HospitalClinicalOrderDetailDto : HospitalClinicalOrderSummaryDto
{
    public Guid? SpecimenId { get; set; }
    public string? SpecimenCode { get; set; }
    public string? SpecimenStatus { get; set; }
    public DateTime? CollectedAtLocal { get; set; }
    public DateTime? ReceivedAtLocal { get; set; }
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? ReportUri { get; set; }
    public string? SignedByUsername { get; set; }
    public DateTime? SignedAtLocal { get; set; }
    public List<HospitalLabResultItemDto> ResultItems { get; set; } = new();
}

public class CreateHospitalClinicalOrderDto
{
    [Required]
    public Guid EncounterId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public Guid ServiceId { get; set; }

    [MaxLength(30)]
    public string? PriorityCode { get; set; }
}

public class RecordHospitalLabResultItemDto
{
    [MaxLength(50)]
    public string? AnalyteCode { get; set; }

    [Required]
    [MaxLength(255)]
    public string AnalyteName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ResultValue { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? ReferenceRange { get; set; }

    [MaxLength(20)]
    public string? AbnormalFlag { get; set; }
}

public class RecordHospitalLabResultDto
{
    [MaxLength(50)]
    public string? SpecimenCode { get; set; }

    [MinLength(1)]
    public List<RecordHospitalLabResultItemDto> ResultItems { get; set; } = new();
}

public class RecordHospitalImagingReportDto
{
    public string? Findings { get; set; }
    public string? Impression { get; set; }

    [MaxLength(1000)]
    public string? ReportUri { get; set; }
}
