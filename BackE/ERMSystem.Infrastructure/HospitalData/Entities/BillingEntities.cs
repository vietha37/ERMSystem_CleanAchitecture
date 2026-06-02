using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERMSystem.Infrastructure.HospitalData.Entities;

[Table("ServiceCatalog", Schema = "billing")]
public class HospitalServiceCatalogEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string ServiceCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
}

[Table("Invoices", Schema = "billing")]
public class HospitalInvoiceEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }

    [MaxLength(30)]
    public string InvoiceStatus { get; set; } = string.Empty;

    [MaxLength(10)]
    public string CurrencyCode { get; set; } = string.Empty;

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }

    public HospitalPatientEntity Patient { get; set; } = null!;
    public HospitalEncounterEntity? Encounter { get; set; }
    public ICollection<HospitalInvoiceItemEntity> InvoiceItems { get; set; } = new List<HospitalInvoiceItemEntity>();
    public ICollection<HospitalPaymentEntity> Payments { get; set; } = new List<HospitalPaymentEntity>();
}

[Table("InvoiceItems", Schema = "billing")]
public class HospitalInvoiceItemEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }
    public Guid? ServiceCatalogId { get; set; }

    [MaxLength(50)]
    public string ItemType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public HospitalInvoiceEntity Invoice { get; set; } = null!;
    public HospitalServiceCatalogEntity? ServiceCatalog { get; set; }
}

[Table("Payments", Schema = "billing")]
public class HospitalPaymentEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    [MaxLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? GatewayProvider { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(30)]
    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime? PaidAtUtc { get; set; }
    public Guid? ReceivedByUserId { get; set; }

    [MaxLength(150)]
    public string? ExternalTransactionId { get; set; }

    public HospitalInvoiceEntity Invoice { get; set; } = null!;
    public HospitalUserEntity? ReceivedByUser { get; set; }
}
