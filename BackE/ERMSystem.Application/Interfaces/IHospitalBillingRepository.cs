using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalBillingRepository
{
    Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        CancellationToken ct = default);

    Task<HospitalInvoiceAggregateSnapshot?> GetByIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<HospitalBillingEligibleEncounterDto[]> GetEligibleEncountersAsync(CancellationToken ct = default);
    Task<HospitalBillingEncounterSnapshot?> GetEncounterForInvoiceAsync(Guid encounterId, CancellationToken ct = default);
    Task<HospitalBillingDashboardSnapshot> GetDashboardSnapshotAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task AddInvoiceAsync(HospitalInvoiceCreateCommand command, CancellationToken ct = default);
    Task AddInvoiceItemAsync(HospitalInvoiceItemCreateCommand command, CancellationToken ct = default);
    Task AddPaymentAsync(HospitalPaymentCreateCommand command, CancellationToken ct = default);
    Task UpdateInvoiceAmountsAsync(Guid invoiceId, string invoiceStatus, decimal subtotalAmount, decimal discountAmount, decimal insuranceAmount, decimal totalAmount, CancellationToken ct = default);
    Task AddOutboxMessageAsync(HospitalBillingOutboxCreateCommand command, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class HospitalInvoiceAggregateSnapshot
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public Guid? EncounterId { get; set; }
    public string? EncounterNumber { get; set; }
    public string? DoctorName { get; set; }
    public string? SpecialtyName { get; set; }
    public string? ClinicName { get; set; }
    public string InvoiceStatus { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public HospitalInvoiceItemSnapshot[] Items { get; set; } = Array.Empty<HospitalInvoiceItemSnapshot>();
    public HospitalPaymentSnapshot[] Payments { get; set; } = Array.Empty<HospitalPaymentSnapshot>();
}

public class HospitalInvoiceItemSnapshot
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

public class HospitalPaymentSnapshot
{
    public Guid PaymentId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAtUtc { get; set; }
    public string? ReceivedByUsername { get; set; }
    public string? ExternalTransactionId { get; set; }
}

public class HospitalBillingEncounterSnapshot
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public Guid? ExistingInvoiceId { get; set; }
    public string? ExistingInvoiceNumber { get; set; }
    public HospitalBillableLineSnapshot[] BillableLines { get; set; } = Array.Empty<HospitalBillableLineSnapshot>();
}

public class HospitalBillingDashboardSnapshot
{
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public decimal IssuedAmountInRange { get; set; }
    public decimal CollectedAmountInRange { get; set; }
    public decimal OutstandingBalanceAmount { get; set; }
}

public class HospitalBillableLineSnapshot
{
    public Guid? ServiceCatalogId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}

public class HospitalInvoiceCreateCommand
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public string InvoiceStatus { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssuedAtUtc { get; set; }
}

public class HospitalInvoiceItemCreateCommand
{
    public Guid InvoiceItemId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ServiceCatalogId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}

public class HospitalPaymentCreateCommand
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAtUtc { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public string? ExternalTransactionId { get; set; }
}

public class HospitalBillingOutboxCreateCommand
{
    public Guid OutboxMessageId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AvailableAtUtc { get; set; }
}
