using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.ValueObjects;

/// <summary>
/// Value Object đại diện cho số hồ sơ bệnh án (Medical Record Number - MRN).
/// Định dạng chuẩn: MRN-XXXXXXXX
/// </summary>
public sealed class MedicalRecordNumber : ValueObject
{
    public string Value { get; }

    public MedicalRecordNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Medical record number cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public static Result<MedicalRecordNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<MedicalRecordNumber>.Failure("Medical record number is required.");

        return Result<MedicalRecordNumber>.Success(new MedicalRecordNumber(value));
    }

    public static MedicalRecordNumber GenerateFromGuid(Guid id)
    {
        return new MedicalRecordNumber($"MRN-{id.ToString("N")[..8].ToUpperInvariant()}");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
