using System;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Events;

/// <summary>
/// Phát ra khi một lịch hẹn khám bệnh mới được tạo.
/// </summary>
public sealed record AppointmentCreatedDomainEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime AppointmentDate,
    string Status,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi trạng thái của một lịch hẹn bị thay đổi (Pending -> Completed, Cancelled, v.v.).
/// </summary>
public sealed record AppointmentStatusChangedDomainEvent(
    Guid AppointmentId,
    string PreviousStatus,
    string NewStatus,
    string? Reason,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi thời gian lịch hẹn được dời sang thời điểm khác.
/// </summary>
public sealed record AppointmentRescheduledDomainEvent(
    Guid AppointmentId,
    DateTime PreviousDate,
    DateTime NewDate,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
