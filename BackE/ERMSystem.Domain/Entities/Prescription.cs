using System;
using System.Collections.Generic;
using System.Linq;
using ERMSystem.Domain.Common;
using ERMSystem.Domain.Events;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Aggregate Root đại diện cho Đơn thuốc (Prescription).
/// Quản lý danh sách các mục thuốc trong đơn (PrescriptionItems) và đảm bảo tính toàn vẹn.
/// </summary>
public sealed class Prescription : AggregateRoot<Guid>
{
    public Guid MedicalRecordId { get; private set; }

    /// <summary>
    /// Đồng bộ với CreatedAtUtc của BaseEntity.
    /// </summary>
    public DateTime CreatedAt
    {
        get => CreatedAtUtc;
        private set => CreatedAtUtc = value;
    }

    public MedicalRecord MedicalRecord { get; private set; } = null!;
    public ICollection<PrescriptionItem> PrescriptionItems { get; private set; } = new List<PrescriptionItem>();

    // Parameterless constructor cho ORM
    private Prescription() { }

    public Prescription(
        Guid id,
        Guid medicalRecordId,
        DateTime? createdAtUtc = null,
        IEnumerable<PrescriptionItem>? items = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        MedicalRecordId = medicalRecordId;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;

        if (items != null)
        {
            foreach (var item in items)
            {
                PrescriptionItems.Add(item);
            }
        }
    }

    /// <summary>
    /// Factory tạo đơn thuốc mới.
    /// </summary>
    public static Prescription Create(Guid medicalRecordId, Guid? id = null)
    {
        if (medicalRecordId == Guid.Empty)
            throw new ArgumentException("Mã hồ sơ bệnh án không được để trống.", nameof(medicalRecordId));

        var prescriptionId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        var prescription = new Prescription(prescriptionId, medicalRecordId, DateTime.UtcNow);

        prescription.RaiseDomainEvent(new PrescriptionCreatedDomainEvent(
            prescription.Id,
            prescription.MedicalRecordId,
            DateTime.UtcNow));

        return prescription;
    }

    /// <summary>
    /// Khôi phục từ cơ sở dữ liệu.
    /// </summary>
    public static Prescription Reconstitute(
        Guid id,
        Guid medicalRecordId,
        DateTime? createdAtUtc = null,
        IEnumerable<PrescriptionItem>? items = null)
    {
        return new Prescription(id, medicalRecordId, createdAtUtc, items);
    }

    /// <summary>
    /// Thêm một mục thuốc vào đơn.
    /// </summary>
    public PrescriptionItem AddItem(Guid medicineId, string dosage, string duration)
    {
        if (medicineId == Guid.Empty)
            throw new ArgumentException("Mã thuốc không được để trống.", nameof(medicineId));

        if (string.IsNullOrWhiteSpace(dosage))
            throw new ArgumentException("Liều dùng không được để trống.", nameof(dosage));

        if (string.IsNullOrWhiteSpace(duration))
            throw new ArgumentException("Thời gian dùng thuốc không được để trống.", nameof(duration));

        var item = PrescriptionItem.Create(Id, medicineId, dosage, duration);
        PrescriptionItems.Add(item);
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PrescriptionItemAddedDomainEvent(
            Id,
            item.Id,
            item.MedicineId,
            item.Dosage,
            item.Duration,
            DateTime.UtcNow));

        return item;
    }

    /// <summary>
    /// Xóa một mục thuốc khỏi đơn theo ID mục.
    /// </summary>
    public bool RemoveItem(Guid itemId)
    {
        var item = PrescriptionItems.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return false;

        PrescriptionItems.Remove(item);
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PrescriptionItemRemovedDomainEvent(Id, itemId, DateTime.UtcNow));
        return true;
    }
}
