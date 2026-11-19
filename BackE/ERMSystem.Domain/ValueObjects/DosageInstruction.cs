using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;

namespace ERMSystem.Domain.ValueObjects;

/// <summary>
/// Value Object đại diện cho hướng dẫn liều dùng và thời gian dùng thuốc trong đơn thuốc.
/// </summary>
public sealed class DosageInstruction : ValueObject
{
    public string Dosage { get; }
    public string Duration { get; }

    public DosageInstruction(string dosage, string duration)
    {
        if (string.IsNullOrWhiteSpace(dosage))
            throw new ArgumentException("Dosage cannot be empty.", nameof(dosage));

        if (string.IsNullOrWhiteSpace(duration))
            throw new ArgumentException("Duration cannot be empty.", nameof(duration));

        Dosage = dosage.Trim();
        Duration = duration.Trim();
    }

    public static Result<DosageInstruction> Create(string? dosage, string? duration)
    {
        if (string.IsNullOrWhiteSpace(dosage))
            return Result<DosageInstruction>.Failure("Dosage is required.");

        if (string.IsNullOrWhiteSpace(duration))
            return Result<DosageInstruction>.Failure("Duration is required.");

        return Result<DosageInstruction>.Success(new DosageInstruction(dosage, duration));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Dosage.ToLowerInvariant();
        yield return Duration.ToLowerInvariant();
    }

    public override string ToString() => $"{Dosage}, trong {Duration}";
}
