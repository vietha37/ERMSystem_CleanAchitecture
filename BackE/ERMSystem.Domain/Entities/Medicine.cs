using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Thực thể Thuốc (Medicine).
/// </summary>
public sealed class Medicine : BaseEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<PrescriptionItem> PrescriptionItems { get; private set; } = new List<PrescriptionItem>();

    // Parameterless constructor cho ORM
    private Medicine() { }

    public Medicine(Guid id, string name, string description, DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public static Medicine Create(string name, string description, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên thuốc không được để trống.", nameof(name));

        var medicineId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        return new Medicine(medicineId, name.Trim(), description?.Trim() ?? string.Empty, DateTime.UtcNow);
    }

    public static Medicine Reconstitute(Guid id, string name, string description, DateTime? createdAtUtc = null)
    {
        return new Medicine(id, name, description, createdAtUtc);
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên thuốc không được để trống.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
