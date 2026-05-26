using System.ComponentModel.DataAnnotations;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.DTOs;

public class HospitalInvoiceWorklistRequestDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [MaxLength(30)]
    public string? InvoiceStatus { get; set; }

    [MaxLength(200)]
    public string? TextSearch { get; set; }
}

public class HospitalInvoiceSummaryDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public Guid? EncounterId { get; set; }
    public string? EncounterNumber { get; set; }
    public string InvoiceStatus { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public int TotalItems { get; set; }
    public int TotalPayments { get; set; }
    public DateTime IssuedAtLocal { get; set; }
    public DateTime? DueAtLocal { get; set; }
}

public class HospitalInvoiceItemDto
{
    public Guid InvoiceItemId { get; set; }
    public Guid? ServiceCatalogId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}

public class HospitalPaymentDto
{
    public Guid PaymentId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAtLocal { get; set; }
    public string? ReceivedByUsername { get; set; }
    public string? ExternalTransactionId { get; set; }
}

public class HospitalPaymentIntentDto
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public string CheckoutToken { get; set; } = string.Empty;
    public string InstructionText { get; set; } = string.Empty;
    public DateTime CreatedAtLocal { get; set; }
}

public class HospitalPaymentReconciliationSummaryDto
{
    public DateTime GeneratedAtLocal { get; set; }
    public int PendingPayments { get; set; }
    public int CapturedPayments { get; set; }
    public int FailedPayments { get; set; }
    public int RefundedPayments { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CapturedAmount { get; set; }
    public decimal FailedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public int MissingExternalTransactionCount { get; set; }
    public List<HospitalPaymentDto> RecentPayments { get; set; } = new();
}

public class HospitalInvoiceDetailDto : HospitalInvoiceSummaryDto
{
    public string? DoctorName { get; set; }
    public string? SpecialtyName { get; set; }
    public string? ClinicName { get; set; }
    public List<HospitalInvoiceItemDto> Items { get; set; } = new();
    public List<HospitalPaymentDto> Payments { get; set; } = new();
}

public class HospitalBillingEligibleEncounterDto
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public int CompletedLabOrders { get; set; }
    public int CompletedImagingOrders { get; set; }
    public int BilledPrescriptionItems { get; set; }
    public Guid? ExistingInvoiceId { get; set; }
    public string? ExistingInvoiceNumber { get; set; }
    public DateTime StartedAtLocal { get; set; }
}

public class CreateHospitalInvoiceDto
{
    [Required]
    public Guid EncounterId { get; set; }

    [Range(0, 999999999)]
    public decimal DiscountAmount { get; set; }

    [Range(0, 999999999)]
    public decimal InsuranceAmount { get; set; }
}

public class ReceiveHospitalPaymentDto
{
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [MaxLength(150)]
    public string? ExternalTransactionId { get; set; }
}

public class CreateHospitalPaymentIntentDto
{
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [MaxLength(150)]
    public string? ExternalTransactionId { get; set; }
}

public class ConfirmHospitalPaymentCallbackDto
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ExternalTransactionId { get; set; }

    [Required]
    [MaxLength(30)]
    public string GatewayStatus { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal? Amount { get; set; }
}
