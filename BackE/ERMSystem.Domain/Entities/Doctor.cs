using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Thực thể Bác sĩ (Doctor).
/// </summary>
public sealed class Doctor : BaseEntity<Guid>
{
    public string FullName { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;

    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    // Parameterless constructor cho ORM
    private Doctor() { }

    public Doctor(Guid id, string fullName, string specialty, DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        FullName = fullName ?? string.Empty;
        Specialty = specialty ?? string.Empty;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public static Doctor Create(string fullName, string specialty, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bác sĩ không được để trống.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(specialty))
            throw new ArgumentException("Chuyên khoa không được để trống.", nameof(specialty));

        var doctorId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        return new Doctor(doctorId, fullName.Trim(), specialty.Trim(), DateTime.UtcNow);
    }

    public static Doctor Reconstitute(Guid id, string fullName, string specialty, DateTime? createdAtUtc = null)
    {
        return new Doctor(id, fullName, specialty, createdAtUtc);
    }

    public void Update(string fullName, string specialty)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bác sĩ không được để trống.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(specialty))
            throw new ArgumentException("Chuyên khoa không được để trống.", nameof(specialty));

        FullName = fullName.Trim();
        Specialty = specialty.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
