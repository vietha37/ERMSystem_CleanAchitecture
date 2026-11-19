using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.ValueObjects;

/// <summary>
/// Value Object đại diện thông tin người liên hệ khẩn cấp của bệnh nhân.
/// Bất biến và so sánh theo giá trị cấu thành.
/// </summary>
public sealed class EmergencyContact : ValueObject
{
    public string FullName { get; }
    public string Phone { get; }
    public string Relationship { get; }

    public EmergencyContact(string fullName, string phone, string relationship)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Emergency contact name cannot be empty.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Emergency contact phone cannot be empty.", nameof(phone));

        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Emergency contact relationship cannot be empty.", nameof(relationship));

        FullName = fullName.Trim();
        Phone = phone.Trim();
        Relationship = relationship.Trim();
    }

    public static Result<EmergencyContact> Create(string? fullName, string? phone, string? relationship)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result<EmergencyContact>.Failure("Emergency contact name is required.");

        if (string.IsNullOrWhiteSpace(phone))
            return Result<EmergencyContact>.Failure("Emergency contact phone is required.");

        if (string.IsNullOrWhiteSpace(relationship))
            return Result<EmergencyContact>.Failure("Emergency contact relationship is required.");

        return Result<EmergencyContact>.Success(new EmergencyContact(fullName, phone, relationship));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FullName.ToLowerInvariant();
        yield return Phone;
        yield return Relationship.ToLowerInvariant();
    }

    public override string ToString() => $"{FullName} ({Relationship}) - {Phone}";
}
