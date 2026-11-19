using System;
using ERMSystem.Domain.ValueObjects;
using Xunit;

namespace ERMSystem.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void EmergencyContact_ValidData_CreatesSuccessfully()
    {
        var contact = new EmergencyContact("Tran Van B", "0901234567", "Anh trai");

        Assert.Equal("Tran Van B", contact.FullName);
        Assert.Equal("0901234567", contact.Phone);
        Assert.Equal("Anh trai", contact.Relationship);
    }

    [Theory]
    [InlineData("", "0901234567", "Anh")]
    [InlineData("Nguyen A", "", "Anh")]
    [InlineData("Nguyen A", "0901234567", "")]
    public void EmergencyContact_InvalidData_ThrowsArgumentException(string name, string phone, string rel)
    {
        Assert.Throws<ArgumentException>(() => new EmergencyContact(name, phone, rel));
    }

    [Fact]
    public void EmergencyContact_StructuralEquality_SameValuesAreEqual()
    {
        var contact1 = new EmergencyContact("Tran Van B", "0901234567", "Anh trai");
        var contact2 = new EmergencyContact("tran van b", "0901234567", "Anh Trai");

        Assert.Equal(contact1, contact2);
        Assert.True(contact1 == contact2);
    }

    [Fact]
    public void EmergencyContact_DifferentValues_AreNotEqual()
    {
        var contact1 = new EmergencyContact("Tran Van B", "0901234567", "Anh trai");
        var contact2 = new EmergencyContact("Tran Van C", "0901234567", "Anh trai");

        Assert.NotEqual(contact1, contact2);
        Assert.True(contact1 != contact2);
    }

    [Fact]
    public void DosageInstruction_ValidData_CreatesSuccessfully()
    {
        var dosage = new DosageInstruction("1 vien sau an", "7 ngay");

        Assert.Equal("1 vien sau an", dosage.Dosage);
        Assert.Equal("7 ngay", dosage.Duration);
    }

    [Theory]
    [InlineData("", "7 ngay")]
    [InlineData("1 vien", "")]
    public void DosageInstruction_InvalidData_ThrowsArgumentException(string dosage, string duration)
    {
        Assert.Throws<ArgumentException>(() => new DosageInstruction(dosage, duration));
    }

    [Fact]
    public void DosageInstruction_StructuralEquality_SameValuesAreEqual()
    {
        var d1 = new DosageInstruction("1 vien", "5 ngay");
        var d2 = new DosageInstruction("1 Vien", "5 Ngay");

        Assert.Equal(d1, d2);
        Assert.True(d1 == d2);
    }

    [Fact]
    public void MedicalRecordNumber_ValidData_FormatsUpperCase()
    {
        var mrn = new MedicalRecordNumber("mrn-a1b2c3d4");

        Assert.Equal("MRN-A1B2C3D4", mrn.Value);
    }

    [Fact]
    public void MedicalRecordNumber_GenerateFromGuid_ReturnsValidFormat()
    {
        var guid = Guid.NewGuid();
        var mrn = MedicalRecordNumber.GenerateFromGuid(guid);

        Assert.StartsWith("MRN-", mrn.Value);
        Assert.Equal(12, mrn.Value.Length); // "MRN-" (4) + 8 hex chars
    }

    [Fact]
    public void MedicalRecordNumber_EmptyValue_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MedicalRecordNumber(""));
    }
}
