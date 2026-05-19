using System;
using System.Collections.Generic;

namespace ERMSystem.Application.DTOs
{
    public class HospitalPatientPortalOverviewDto
    {
        public HospitalPatientPortalProfileDto Profile { get; set; } = new();
        public IReadOnlyList<HospitalPatientPortalAppointmentDto> UpcomingAppointments { get; set; } = Array.Empty<HospitalPatientPortalAppointmentDto>();
        public IReadOnlyList<HospitalPatientPortalAppointmentDto> RecentAppointments { get; set; } = Array.Empty<HospitalPatientPortalAppointmentDto>();
        public IReadOnlyList<HospitalPatientPortalPrescriptionDto> RecentPrescriptions { get; set; } = Array.Empty<HospitalPatientPortalPrescriptionDto>();
        public IReadOnlyList<HospitalPatientPortalClinicalOrderDto> RecentClinicalOrders { get; set; } = Array.Empty<HospitalPatientPortalClinicalOrderDto>();
        public IReadOnlyList<HospitalPatientPortalInvoiceDto> RecentInvoices { get; set; } = Array.Empty<HospitalPatientPortalInvoiceDto>();
    }

    public class HospitalPatientVisitHistoryResultDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public IReadOnlyList<HospitalPatientVisitHistoryItemDto> Items { get; set; } = Array.Empty<HospitalPatientVisitHistoryItemDto>();
    }

    public class HospitalPatientVisitHistoryItemDto
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string AppointmentStatus { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public string BookingChannel { get; set; } = string.Empty;
        public DateTime AppointmentStartLocal { get; set; }
        public DateTime? AppointmentEndLocal { get; set; }
        public DateTime? CheckInTimeLocal { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public Guid? EncounterId { get; set; }
        public string? EncounterNumber { get; set; }
        public string? EncounterStatus { get; set; }
        public DateTime? EncounterStartedLocal { get; set; }
        public DateTime? EncounterEndedLocal { get; set; }
        public string? PrimaryDiagnosisName { get; set; }
        public string? ClinicalSummary { get; set; }
        public int PrescriptionCount { get; set; }
        public int ClinicalOrderCount { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal OutstandingBalanceAmount { get; set; }
    }

    public class HospitalPatientPortalProfileDto
    {
        public Guid PatientId { get; set; }
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string PortalStatus { get; set; } = string.Empty;
        public DateTime ActivatedAtUtc { get; set; }
    }

    public class HospitalPatientPortalAppointmentDto
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public string BookingChannel { get; set; } = string.Empty;
        public DateTime AppointmentStartLocal { get; set; }
        public DateTime? AppointmentEndLocal { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
    }

    public class HospitalPatientPortalPrescriptionDto
    {
        public Guid PrescriptionId { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EncounterNumber { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string? PrimaryDiagnosisName { get; set; }
        public int TotalItems { get; set; }
        public DateTime CreatedAtLocal { get; set; }
        public DateTime? DispensedAtLocal { get; set; }
        public string? Notes { get; set; }
        public IReadOnlyList<HospitalPatientPortalPrescriptionItemDto> Items { get; set; } = Array.Empty<HospitalPatientPortalPrescriptionItemDto>();
    }

    public class HospitalPatientPortalPrescriptionItemDto
    {
        public Guid PrescriptionItemId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string DrugCode { get; set; } = string.Empty;
        public string DoseInstruction { get; set; } = string.Empty;
        public string? Route { get; set; }
        public string? Frequency { get; set; }
        public int? DurationDays { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class HospitalPatientPortalClinicalOrderDto
    {
        public Guid ClinicalOrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EncounterNumber { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCode { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime RequestedAtLocal { get; set; }
        public DateTime? CompletedAtLocal { get; set; }
        public string? SummaryText { get; set; }
        public string? Findings { get; set; }
        public string? Impression { get; set; }
        public string? ReportUri { get; set; }
        public IReadOnlyList<HospitalPatientPortalLabResultItemDto> ResultItems { get; set; } = Array.Empty<HospitalPatientPortalLabResultItemDto>();
    }

    public class HospitalPatientPortalLabResultItemDto
    {
        public Guid ResultItemId { get; set; }
        public string AnalyteName { get; set; } = string.Empty;
        public string? ResultValue { get; set; }
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public string? AbnormalFlag { get; set; }
    }

    public class HospitalPatientPortalInvoiceDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceStatus { get; set; } = string.Empty;
        public string? EncounterNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public int TotalItems { get; set; }
        public int TotalPayments { get; set; }
        public DateTime IssuedAtLocal { get; set; }
        public DateTime? DueAtLocal { get; set; }
        public IReadOnlyList<HospitalPatientPortalInvoiceItemDto> Items { get; set; } = Array.Empty<HospitalPatientPortalInvoiceItemDto>();
        public IReadOnlyList<HospitalPatientPortalPaymentDto> Payments { get; set; } = Array.Empty<HospitalPatientPortalPaymentDto>();
    }

    public class HospitalPatientPortalInvoiceItemDto
    {
        public Guid InvoiceItemId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }
    }

    public class HospitalPatientPortalPaymentDto
    {
        public Guid PaymentId { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaidAtLocal { get; set; }
    }
}
