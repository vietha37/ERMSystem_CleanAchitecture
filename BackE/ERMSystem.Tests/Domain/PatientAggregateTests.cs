using System;
using System.Linq;
using ERMSystem.Domain.Entities;
using ERMSystem.Domain.Events;
using ERMSystem.Domain.ValueObjects;
using Xunit;

namespace ERMSystem.Tests.Domain;

public class PatientAggregateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsPatientAndRaisesPatientCreatedDomainEvent()
    {
        var fullName = "Nguyen Van A";
        var dob = new DateTime(1990, 1, 1);
        var gender = "Nam";
        var phone = "0901234567";
        var address = "123 Le Loi, TP.HCM";

        var patient = Patient.Create(fullName, dob, gender, phone, address);

        Assert.NotNull(patient);
        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.Equal(fullName, patient.FullName);
        Assert.Equal(dob, patient.DateOfBirth);
        Assert.Equal(gender, patient.Gender);
        Assert.Equal(phone, patient.Phone);
        Assert.Equal(address, patient.Address);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientCreatedDomainEvent>(patient.DomainEvents.First());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyFullName_ThrowsArgumentException(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
            Patient.Create(invalidName!, new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyPhone_ThrowsArgumentException(string? invalidPhone)
    {
        Assert.Throws<ArgumentException>(() =>
            Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", invalidPhone!, "TP.HCM"));
    }

    [Fact]
    public void UpdateProfile_WithValidData_UpdatesPropertiesAndRaisesPatientUpdatedDomainEvent()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        patient.ClearDomainEvents();

        var newName = "Nguyen Van B";
        var newDob = new DateTime(1992, 5, 20);
        var newGender = "Nu";
        var newPhone = "0987654321";
        var newAddress = "456 Nguyen Hue, TP.HCM";

        patient.UpdateProfile(newName, newDob, newGender, newPhone, newAddress);

        Assert.Equal(newName, patient.FullName);
        Assert.Equal(newDob, patient.DateOfBirth);
        Assert.Equal(newGender, patient.Gender);
        Assert.Equal(newPhone, patient.Phone);
        Assert.Equal(newAddress, patient.Address);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientUpdatedDomainEvent>(patient.DomainEvents.First());
    }

    [Fact]
    public void UpdateProfile_WithEmptyFullName_ThrowsArgumentException()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");

        Assert.Throws<ArgumentException>(() =>
            patient.UpdateProfile("", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM"));
    }

    [Fact]
    public void LinkAppUser_ValidUserId_SetsAppUserIdAndRaisesPatientAppUserLinkedDomainEvent()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        patient.ClearDomainEvents();

        var userId = Guid.NewGuid();
        patient.LinkAppUser(userId);

        Assert.Equal(userId, patient.AppUserId);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientAppUserLinkedDomainEvent>(patient.DomainEvents.First());
    }

    [Fact]
    public void LinkAppUser_EmptyGuid_ThrowsArgumentException()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");

        Assert.Throws<ArgumentException>(() => patient.LinkAppUser(Guid.Empty));
    }

    [Fact]
    public void LinkAppUser_AlreadyLinkedToDifferentUser_ThrowsInvalidOperationException()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        patient.LinkAppUser(user1);

        Assert.Throws<InvalidOperationException>(() => patient.LinkAppUser(user2));
    }

    [Fact]
    public void LinkAppUser_SameUser_SucceedsWithoutError()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var user1 = Guid.NewGuid();

        patient.LinkAppUser(user1);
        patient.LinkAppUser(user1);

        Assert.Equal(user1, patient.AppUserId);
    }

    [Fact]
    public void UnlinkAppUser_WhenLinked_ClearsAppUserIdAndRaisesPatientAppUserUnlinkedDomainEvent()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var user1 = Guid.NewGuid();
        patient.LinkAppUser(user1);
        patient.ClearDomainEvents();

        patient.UnlinkAppUser();

        Assert.Null(patient.AppUserId);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientAppUserUnlinkedDomainEvent>(patient.DomainEvents.First());
    }

    [Fact]
    public void UnlinkAppUser_WhenNotLinked_DoesNothing()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        patient.ClearDomainEvents();

        patient.UnlinkAppUser();

        Assert.Null(patient.AppUserId);
        Assert.Empty(patient.DomainEvents);
    }

    [Fact]
    public void MergeWith_SamePatient_ThrowsInvalidOperationException()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");

        Assert.Throws<InvalidOperationException>(() => patient.MergeWith(patient));
    }

    [Fact]
    public void MergeWith_ConflictingAppUserIds_ThrowsInvalidOperationException()
    {
        var target = Patient.Create("Nguyen Van Target", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var source = Patient.Create("Nguyen Van Source", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");

        target.LinkAppUser(Guid.NewGuid());
        source.LinkAppUser(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => target.MergeWith(source));
    }

    [Fact]
    public void MergeWith_TargetHasNoUser_AdoptsSourceAppUserId()
    {
        var target = Patient.Create("Nguyen Van Target", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var source = Patient.Create("Nguyen Van Source", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var sourceUserId = Guid.NewGuid();
        source.LinkAppUser(sourceUserId);

        target.MergeWith(source, "AdminTester");

        Assert.Equal(sourceUserId, target.AppUserId);
        var mergedEvent = target.DomainEvents.OfType<PatientMergedDomainEvent>().FirstOrDefault();
        Assert.NotNull(mergedEvent);
        Assert.Equal(target.Id, mergedEvent.TargetPatientId);
        Assert.Equal(source.Id, mergedEvent.SourcePatientId);
        Assert.Equal("AdminTester", mergedEvent.Actor);
    }

    [Fact]
    public void MergeWith_FillsMissingFieldsFromSource()
    {
        var target = Patient.Reconstitute(Guid.NewGuid(), "", default, "", "", "");
        var source = Patient.Create("Nguyen Van Source", new DateTime(1985, 3, 10), "Nam", "0909999999", "Ha Noi",
            "Emergency Contact A", "0911222333", "Vo");

        target.MergeWith(source);

        Assert.Equal("Nguyen Van Source", target.FullName);
        Assert.Equal(new DateTime(1985, 3, 10), target.DateOfBirth);
        Assert.Equal("Nam", target.Gender);
        Assert.Equal("0909999999", target.Phone);
        Assert.Equal("Ha Noi", target.Address);
        Assert.Equal("Emergency Contact A", target.EmergencyContactName);
        Assert.Equal("0911222333", target.EmergencyContactPhone);
        Assert.Equal("Vo", target.EmergencyContactRelationship);
    }

    [Fact]
    public void SetEmergencyContact_ValidContact_SetsFieldsAndEmergencyContactInfo()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        var contact = new EmergencyContact("Tran Thi B", "0988888888", "Vo");

        patient.SetEmergencyContact(contact);

        Assert.Equal("Tran Thi B", patient.EmergencyContactName);
        Assert.Equal("0988888888", patient.EmergencyContactPhone);
        Assert.Equal("Vo", patient.EmergencyContactRelationship);
        Assert.NotNull(patient.EmergencyContactInfo);
        Assert.Equal("Tran Thi B", patient.EmergencyContactInfo.FullName);
    }

    [Fact]
    public void ClearEmergencyContact_ClearsAllFields()
    {
        var patient = Patient.Create("Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");
        patient.SetEmergencyContact(new EmergencyContact("Tran Thi B", "0988888888", "Vo"));

        patient.ClearEmergencyContact();

        Assert.Null(patient.EmergencyContactName);
        Assert.Null(patient.EmergencyContactPhone);
        Assert.Null(patient.EmergencyContactRelationship);
        Assert.Null(patient.EmergencyContactInfo);
    }

    [Fact]
    public void Reconstitute_DoesNotEmitDomainEvents()
    {
        var patient = Patient.Reconstitute(Guid.NewGuid(), "Nguyen Van A", new DateTime(1990, 1, 1), "Nam", "0901234567", "TP.HCM");

        Assert.Empty(patient.DomainEvents);
    }
}
