using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Thực thể Hồ sơ Bệnh án khám bệnh (MedicalRecord).
/// </summary>
public sealed class MedicalRecord : BaseEntity<Guid>
{
    public Guid AppointmentId { get; private set; }

    /// <summary>
    /// Đồng bộ với CreatedAtUtc của BaseEntity.
    /// </summary>
    public DateTime CreatedAt
    {
        get => CreatedAtUtc;
        private set => CreatedAtUtc = value;
    }

    public string Symptoms { get; private set; } = string.Empty;
    public string Diagnosis { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public Appointment Appointment { get; private set; } = null!;
    public ICollection<Prescription> Prescriptions { get; private set; } = new List<Prescription>();

    // Parameterless constructor cho ORM
    private MedicalRecord() { }

    public MedicalRecord(
        Guid id,
        Guid appointmentId,
        string symptoms,
        string diagnosis,
        string notes,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        AppointmentId = appointmentId;
        Symptoms = symptoms ?? string.Empty;
        Diagnosis = diagnosis ?? string.Empty;
        Notes = notes ?? string.Empty;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public static MedicalRecord Create(
        Guid appointmentId,
        string symptoms,
        string diagnosis,
        string notes,
        Guid? id = null)
    {
        if (appointmentId == Guid.Empty)
            throw new ArgumentException("Mã lịch hẹn không được để trống.", nameof(appointmentId));

        var recordId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        return new MedicalRecord(
            recordId,
            appointmentId,
            symptoms?.Trim() ?? string.Empty,
            diagnosis?.Trim() ?? string.Empty,
            notes?.Trim() ?? string.Empty,
            DateTime.UtcNow);
    }

    public static MedicalRecord Reconstitute(
        Guid id,
        Guid appointmentId,
        string symptoms,
        string diagnosis,
        string notes,
        DateTime? createdAtUtc = null)
    {
        return new MedicalRecord(id, appointmentId, symptoms, diagnosis, notes, createdAtUtc);
    }

    public void UpdateClinicalNotes(string symptoms, string diagnosis, string notes)
    {
        Symptoms = symptoms?.Trim() ?? string.Empty;
        Diagnosis = diagnosis?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
