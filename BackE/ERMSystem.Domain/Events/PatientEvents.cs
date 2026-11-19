using System;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.Events;

/// <summary>
/// Phát ra khi một hồ sơ bệnh nhân mới được tạo trong hệ thống.
/// </summary>
public sealed record PatientCreatedDomainEvent(
    Guid PatientId,
    string FullName,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi thông tin hành chính của bệnh nhân được cập nhật.
/// </summary>
public sealed record PatientUpdatedDomainEvent(
    Guid PatientId,
    string FullName,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi hai hồ sơ bệnh nhân trùng lặp được sáp nhập.
/// </summary>
public sealed record PatientMergedDomainEvent(
    Guid TargetPatientId,
    Guid SourcePatientId,
    string Actor,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi bệnh nhân được liên kết với tài khoản người dùng portal.
/// </summary>
public sealed record PatientAppUserLinkedDomainEvent(
    Guid PatientId,
    Guid AppUserId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Phát ra khi liên kết tài khoản portal của bệnh nhân bị hủy bỏ.
/// </summary>
public sealed record PatientAppUserUnlinkedDomainEvent(
    Guid PatientId,
    DateTime OccurredOnUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
