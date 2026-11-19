using System;
using System.Linq;
using ERMSystem.Domain.Entities;
using ERMSystem.Domain.Events;
using Xunit;

namespace ERMSystem.Tests.Domain;

public class PrescriptionAggregateTests
{
    [Fact]
    public void Create_WithValidData_RaisesPrescriptionCreatedDomainEvent()
    {
        var recordId = Guid.NewGuid();

        var prescription = Prescription.Create(recordId);

        Assert.NotNull(prescription);
        Assert.NotEqual(Guid.Empty, prescription.Id);
        Assert.Equal(recordId, prescription.MedicalRecordId);
        Assert.Single(prescription.DomainEvents);
        Assert.IsType<PrescriptionCreatedDomainEvent>(prescription.DomainEvents.First());
    }

    [Fact]
    public void Create_WithEmptyMedicalRecordId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Prescription.Create(Guid.Empty));
    }

    [Fact]
    public void AddItem_WithValidData_AddsItemAndRaisesPrescriptionItemAddedDomainEvent()
    {
        var prescription = Prescription.Create(Guid.NewGuid());
        prescription.ClearDomainEvents();

        var medicineId = Guid.NewGuid();
        var item = prescription.AddItem(medicineId, "2 vien moi ngay", "7 ngay");

        Assert.NotNull(item);
        Assert.Equal(medicineId, item.MedicineId);
        Assert.Equal("2 vien moi ngay", item.Dosage);
        Assert.Equal("7 ngay", item.Duration);
        Assert.Single(prescription.PrescriptionItems);
        Assert.Single(prescription.DomainEvents);

        var addedEvent = Assert.IsType<PrescriptionItemAddedDomainEvent>(prescription.DomainEvents.First());
        Assert.Equal(prescription.Id, addedEvent.PrescriptionId);
        Assert.Equal(item.Id, addedEvent.ItemId);
        Assert.Equal(medicineId, addedEvent.MedicineId);
    }

    [Fact]
    public void AddItem_WithEmptyMedicineId_ThrowsArgumentException()
    {
        var prescription = Prescription.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            prescription.AddItem(Guid.Empty, "1 vien", "3 ngay"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void AddItem_WithEmptyDosage_ThrowsArgumentException(string? invalidDosage)
    {
        var prescription = Prescription.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            prescription.AddItem(Guid.NewGuid(), invalidDosage!, "3 ngay"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void AddItem_WithEmptyDuration_ThrowsArgumentException(string? invalidDuration)
    {
        var prescription = Prescription.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            prescription.AddItem(Guid.NewGuid(), "1 vien", invalidDuration!));
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesItemAndRaisesPrescriptionItemRemovedDomainEvent()
    {
        var prescription = Prescription.Create(Guid.NewGuid());
        var item = prescription.AddItem(Guid.NewGuid(), "1 vien", "5 ngay");
        prescription.ClearDomainEvents();

        var removed = prescription.RemoveItem(item.Id);

        Assert.True(removed);
        Assert.Empty(prescription.PrescriptionItems);
        Assert.Single(prescription.DomainEvents);

        var removedEvent = Assert.IsType<PrescriptionItemRemovedDomainEvent>(prescription.DomainEvents.First());
        Assert.Equal(prescription.Id, removedEvent.PrescriptionId);
        Assert.Equal(item.Id, removedEvent.ItemId);
    }

    [Fact]
    public void RemoveItem_NonExistingItem_ReturnsFalseAndDoesNotEmitEvent()
    {
        var prescription = Prescription.Create(Guid.NewGuid());
        prescription.ClearDomainEvents();

        var removed = prescription.RemoveItem(Guid.NewGuid());

        Assert.False(removed);
        Assert.Empty(prescription.DomainEvents);
    }
}
