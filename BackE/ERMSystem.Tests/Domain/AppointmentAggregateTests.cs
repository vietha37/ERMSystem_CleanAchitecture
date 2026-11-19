using System;
using System.Linq;
using ERMSystem.Domain.Constants;
using ERMSystem.Domain.Entities;
using ERMSystem.Domain.Events;
using Xunit;

namespace ERMSystem.Tests.Domain;

public class AppointmentAggregateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsAppointmentAndRaisesAppointmentCreatedDomainEvent()
    {
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var apptDate = DateTime.UtcNow.AddDays(2);

        var appt = Appointment.Create(patientId, doctorId, apptDate, AppointmentStatuses.Pending);

        Assert.NotNull(appt);
        Assert.NotEqual(Guid.Empty, appt.Id);
        Assert.Equal(patientId, appt.PatientId);
        Assert.Equal(doctorId, appt.DoctorId);
        Assert.Equal(apptDate, appt.AppointmentDate);
        Assert.Equal(AppointmentStatuses.Pending, appt.Status);
        Assert.Single(appt.DomainEvents);
        Assert.IsType<AppointmentCreatedDomainEvent>(appt.DomainEvents.First());
    }

    [Fact]
    public void Create_WithEmptyPatientId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Appointment.Create(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithEmptyDoctorId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Appointment.Create(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithInvalidStatus_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "NonExistentStatus"));
    }

    [Fact]
    public void Complete_WhenPending_TransitionsToCompletedAndRaisesEvent()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.ClearDomainEvents();

        appt.Complete();

        Assert.Equal(AppointmentStatuses.Completed, appt.Status);
        Assert.Single(appt.DomainEvents);
        var statusEvent = Assert.IsType<AppointmentStatusChangedDomainEvent>(appt.DomainEvents.First());
        Assert.Equal(AppointmentStatuses.Pending, statusEvent.PreviousStatus);
        Assert.Equal(AppointmentStatuses.Completed, statusEvent.NewStatus);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_DoesNotRaiseNewEvent()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Complete();
        appt.ClearDomainEvents();

        appt.Complete();

        Assert.Equal(AppointmentStatuses.Completed, appt.Status);
        Assert.Empty(appt.DomainEvents);
    }

    [Fact]
    public void Complete_WhenCancelled_ThrowsInvalidOperationException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Cancel("Benh nhan ban");

        Assert.Throws<InvalidOperationException>(() => appt.Complete());
    }

    [Fact]
    public void Cancel_WhenPending_TransitionsToCancelledAndRaisesEvent()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.ClearDomainEvents();

        appt.Cancel("Ly do dot xuat");

        Assert.Equal(AppointmentStatuses.Cancelled, appt.Status);
        Assert.Single(appt.DomainEvents);
        var statusEvent = Assert.IsType<AppointmentStatusChangedDomainEvent>(appt.DomainEvents.First());
        Assert.Equal(AppointmentStatuses.Pending, statusEvent.PreviousStatus);
        Assert.Equal(AppointmentStatuses.Cancelled, statusEvent.NewStatus);
        Assert.Equal("Ly do dot xuat", statusEvent.Reason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_DoesNotRaiseNewEvent()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Cancel("Ly do 1");
        appt.ClearDomainEvents();

        appt.Cancel("Ly do 2");

        Assert.Equal(AppointmentStatuses.Cancelled, appt.Status);
        Assert.Empty(appt.DomainEvents);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Complete();

        Assert.Throws<InvalidOperationException>(() => appt.Cancel("Khong the huy"));
    }

    [Fact]
    public void Reschedule_WhenPending_UpdatesDateAndRaisesEvent()
    {
        var originalDate = DateTime.UtcNow.AddDays(1);
        var newDate = DateTime.UtcNow.AddDays(3);
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), originalDate);
        appt.ClearDomainEvents();

        appt.Reschedule(newDate);

        Assert.Equal(newDate, appt.AppointmentDate);
        Assert.Single(appt.DomainEvents);
        var reschedEvent = Assert.IsType<AppointmentRescheduledDomainEvent>(appt.DomainEvents.First());
        Assert.Equal(originalDate, reschedEvent.PreviousDate);
        Assert.Equal(newDate, reschedEvent.NewDate);
    }

    [Fact]
    public void Reschedule_WhenCompleted_ThrowsInvalidOperationException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Complete();

        Assert.Throws<InvalidOperationException>(() => appt.Reschedule(DateTime.UtcNow.AddDays(5)));
    }

    [Fact]
    public void Reschedule_WhenCancelled_ThrowsInvalidOperationException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Cancel("Cancelled");

        Assert.Throws<InvalidOperationException>(() => appt.Reschedule(DateTime.UtcNow.AddDays(5)));
    }

    [Fact]
    public void ReassignDoctor_ValidDoctorId_UpdatesDoctorId()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        var newDoctorId = Guid.NewGuid();

        appt.ReassignDoctor(newDoctorId);

        Assert.Equal(newDoctorId, appt.DoctorId);
    }

    [Fact]
    public void ReassignDoctor_EmptyGuid_ThrowsArgumentException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        Assert.Throws<ArgumentException>(() => appt.ReassignDoctor(Guid.Empty));
    }

    [Fact]
    public void UpdateDetails_ValidDetails_UpdatesProperties()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        var newPatientId = Guid.NewGuid();
        var newDoctorId = Guid.NewGuid();
        var newDate = DateTime.UtcNow.AddDays(4);

        appt.UpdateDetails(newPatientId, newDoctorId, newDate, AppointmentStatuses.Completed);

        Assert.Equal(newPatientId, appt.PatientId);
        Assert.Equal(newDoctorId, appt.DoctorId);
        Assert.Equal(newDate, appt.AppointmentDate);
        Assert.Equal(AppointmentStatuses.Completed, appt.Status);
    }

    [Fact]
    public void UpdateDetails_CancelledToCompleted_ThrowsInvalidOperationException()
    {
        var appt = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        appt.Cancel("Huy");

        Assert.Throws<InvalidOperationException>(() =>
            appt.UpdateDetails(appt.PatientId, appt.DoctorId, appt.AppointmentDate, AppointmentStatuses.Completed));
    }
}
