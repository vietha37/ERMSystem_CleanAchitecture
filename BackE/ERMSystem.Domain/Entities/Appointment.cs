using System;
using ERMSystem.Domain.Common;
using ERMSystem.Domain.Constants;
using ERMSystem.Domain.Events;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Aggregate Root đại diện cho Lịch hẹn khám bệnh (Appointment).
/// Đóng gói trạng thái, vòng đời và kiểm soát chuyển trạng thái (State Machine).
/// </summary>
public sealed class Appointment : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public DateTime AppointmentDate { get; private set; }
    public string Status { get; private set; } = AppointmentStatuses.Pending;

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public Doctor Doctor { get; private set; } = null!;
    public MedicalRecord? MedicalRecord { get; private set; }

    // Parameterless constructor cho ORM
    private Appointment() { }

    public Appointment(
        Guid id,
        Guid patientId,
        Guid doctorId,
        DateTime appointmentDate,
        string status = AppointmentStatuses.Pending,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        PatientId = patientId;
        DoctorId = doctorId;
        AppointmentDate = appointmentDate;
        Status = status;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method tạo lịch hẹn mới, kiểm tra tính hợp lệ và phát sinh domain event.
    /// </summary>
    public static Appointment Create(
        Guid patientId,
        Guid doctorId,
        DateTime appointmentDate,
        string status = AppointmentStatuses.Pending,
        Guid? id = null)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Mã bệnh nhân không được để trống.", nameof(patientId));

        if (doctorId == Guid.Empty)
            throw new ArgumentException("Mã bác sĩ không được để trống.", nameof(doctorId));

        if (!Array.Exists(AppointmentStatuses.All, s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Trạng thái lịch hẹn '{status}' không hợp lệ. Phải là: {string.Join(", ", AppointmentStatuses.All)}.", nameof(status));

        var appointmentId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();

        var appointment = new Appointment(
            appointmentId,
            patientId,
            doctorId,
            appointmentDate,
            status,
            DateTime.UtcNow);

        appointment.RaiseDomainEvent(new AppointmentCreatedDomainEvent(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorId,
            appointment.AppointmentDate,
            appointment.Status,
            DateTime.UtcNow));

        return appointment;
    }

    /// <summary>
    /// Khôi phục từ cơ sở dữ liệu.
    /// </summary>
    public static Appointment Reconstitute(
        Guid id,
        Guid patientId,
        Guid doctorId,
        DateTime appointmentDate,
        string status,
        DateTime? createdAtUtc = null,
        Patient? patient = null,
        Doctor? doctor = null)
    {
        var appt = new Appointment(id, patientId, doctorId, appointmentDate, status, createdAtUtc);
        if (patient != null) appt.Patient = patient;
        if (doctor != null) appt.Doctor = doctor;
        return appt;
    }

    public void SetPatient(Patient patient)
    {
        Patient = patient;
    }

    public void SetDoctor(Doctor doctor)
    {
        Doctor = doctor;
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết của lịch hẹn.
    /// </summary>
    public void UpdateDetails(Guid patientId, Guid doctorId, DateTime appointmentDate, string status)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Mã bệnh nhân không được để trống.", nameof(patientId));

        if (doctorId == Guid.Empty)
            throw new ArgumentException("Mã bác sĩ không được để trống.", nameof(doctorId));

        if (!Array.Exists(AppointmentStatuses.All, s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Trạng thái lịch hẹn '{status}' không hợp lệ.", nameof(status));

        // Kiểm tra chuyển trạng thái hợp lệ
        if (string.Equals(Status, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(status, AppointmentStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Không thể hoàn thành một lịch hẹn đã bị hủy bỏ.");
        }

        var previousStatus = Status;
        PatientId = patientId;
        DoctorId = doctorId;
        AppointmentDate = appointmentDate;
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;

        if (!string.Equals(previousStatus, status, StringComparison.OrdinalIgnoreCase))
        {
            RaiseDomainEvent(new AppointmentStatusChangedDomainEvent(Id, previousStatus, status, null, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Hoàn thành buổi khám lịch hẹn.
    /// </summary>
    public void Complete()
    {
        if (string.Equals(Status, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể hoàn thành một lịch hẹn đã bị hủy bỏ.");

        if (string.Equals(Status, AppointmentStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return;

        var previousStatus = Status;
        Status = AppointmentStatuses.Completed;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new AppointmentStatusChangedDomainEvent(Id, previousStatus, Status, null, DateTime.UtcNow));
    }

    /// <summary>
    /// Hủy bỏ lịch hẹn với lý do.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (string.Equals(Status, AppointmentStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể hủy bỏ một lịch hẹn đã hoàn thành khám.");

        if (string.Equals(Status, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return;

        var previousStatus = Status;
        Status = AppointmentStatuses.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new AppointmentStatusChangedDomainEvent(Id, previousStatus, Status, reason, DateTime.UtcNow));
    }

    /// <summary>
    /// Đổi ngày hẹn khám.
    /// </summary>
    public void Reschedule(DateTime newAppointmentDate)
    {
        if (string.Equals(Status, AppointmentStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể dời ngày khám cho một lịch hẹn đã hoàn thành.");

        if (string.Equals(Status, AppointmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể dời ngày khám cho một lịch hẹn đã bị hủy.");

        var previousDate = AppointmentDate;
        AppointmentDate = newAppointmentDate;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new AppointmentRescheduledDomainEvent(Id, previousDate, newAppointmentDate, DateTime.UtcNow));
    }

    /// <summary>
    /// Đổi bác sĩ phụ trách.
    /// </summary>
    public void ReassignDoctor(Guid newDoctorId)
    {
        if (newDoctorId == Guid.Empty)
            throw new ArgumentException("Mã bác sĩ không hợp lệ.", nameof(newDoctorId));

        DoctorId = newDoctorId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
