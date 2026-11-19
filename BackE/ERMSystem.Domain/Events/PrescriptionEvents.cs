using System;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Events;

/// <summary>
/// Phát ra khi một đơn thuốc mới được tạo cho bệnh án.
/// </summary>
public sealed record PrescriptionCreatedDomainEvent(
    Guid PrescriptionId,
    Guid MedicalRecordId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi một thuốc được thêm vào đơn thuốc.
/// </summary>
public sealed record PrescriptionItemAddedDomainEvent(
    Guid PrescriptionId,
    Guid ItemId,
    Guid MedicineId,
    string Dosage,
    string Duration,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi một thuốc được xóa khỏi đơn thuốc.
/// </summary>
public sealed record PrescriptionItemRemovedDomainEvent(
    Guid PrescriptionId,
    Guid ItemId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
