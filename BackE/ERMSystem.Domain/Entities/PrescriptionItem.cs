using System;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Thực thể mục thuốc trong đơn thuốc (PrescriptionItem).
/// Thuộc về Aggregate Prescription.
/// </summary>
public sealed class PrescriptionItem : BaseEntity<Guid>
{
    public Guid PrescriptionId { get; private set; }
    public Guid MedicineId { get; private set; }
    public string Dosage { get; private set; } = string.Empty;
    public string Duration { get; private set; } = string.Empty;

    // Navigation properties
    public Prescription Prescription { get; private set; } = null!;
    public Medicine Medicine { get; private set; } = null!;

    // Parameterless constructor cho ORM
    private PrescriptionItem() { }

    public PrescriptionItem(
        Guid id,
        Guid prescriptionId,
        Guid medicineId,
        string dosage,
        string duration,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        PrescriptionId = prescriptionId;
        MedicineId = medicineId;
        Dosage = dosage ?? string.Empty;
        Duration = duration ?? string.Empty;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public static PrescriptionItem Create(
        Guid prescriptionId,
        Guid medicineId,
        string dosage,
        string duration,
        Guid? id = null)
    {
        if (prescriptionId == Guid.Empty)
            throw new ArgumentException("Mã đơn thuốc không được để trống.", nameof(prescriptionId));

        if (medicineId == Guid.Empty)
            throw new ArgumentException("Mã thuốc không được để trống.", nameof(medicineId));

        if (string.IsNullOrWhiteSpace(dosage))
            throw new ArgumentException("Liều dùng không được để trống.", nameof(dosage));

        if (string.IsNullOrWhiteSpace(duration))
            throw new ArgumentException("Thời gian dùng thuốc không được để trống.", nameof(duration));

        var itemId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        return new PrescriptionItem(itemId, prescriptionId, medicineId, dosage.Trim(), duration.Trim(), DateTime.UtcNow);
    }

    public static PrescriptionItem Reconstitute(
        Guid id,
        Guid prescriptionId,
        Guid medicineId,
        string dosage,
        string duration,
        DateTime? createdAtUtc = null)
    {
        return new PrescriptionItem(id, prescriptionId, medicineId, dosage, duration, createdAtUtc);
    }

    public void UpdateInstruction(string dosage, string duration)
    {
        if (string.IsNullOrWhiteSpace(dosage))
            throw new ArgumentException("Liều dùng không được để trống.", nameof(dosage));

        if (string.IsNullOrWhiteSpace(duration))
            throw new ArgumentException("Thời gian dùng thuốc không được để trống.", nameof(duration));

        Dosage = dosage.Trim();
        Duration = duration.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
