using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using ERMSystem.Domain.Entities;
using Moq;
using Xunit;

namespace ERMSystem.Tests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IAppointmentRepository> _appointmentRepoMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _appointmentRepoMock = new Mock<IAppointmentRepository>();
            _service = new NotificationService(_appointmentRepoMock.Object);
        }

        [Fact]
        public async Task GetTodayNotificationsAsync_ReturnsAllTodayAppointments_ForAdminRole()
        {
            var today = DateTime.UtcNow.Date.AddHours(10);
            var appointments = new List<Appointment>
            {
                Appointment.Reconstitute(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    today,
                    "Pending",
                    patient: Patient.Reconstitute(Guid.NewGuid(), "Patient A", DateTime.UtcNow, "Other", "000", "Addr"),
                    doctor: Doctor.Reconstitute(Guid.NewGuid(), "Doctor B", "General"))
            };

            _appointmentRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(appointments);

            var result = await _service.GetTodayNotificationsAsync("Admin", "adminuser");

            Assert.NotNull(result);
            Assert.Equal(1, result.UnreadCount);
            Assert.Single(result.Notifications);
            Assert.Equal("Patient A", result.Notifications[0].PatientName);
            Assert.Equal("Doctor B", result.Notifications[0].DoctorName);
        }

        [Fact]
        public async Task GetTodayNotificationsAsync_FiltersByDoctorName_ForDoctorRole()
        {
            var today = DateTime.UtcNow.Date.AddHours(10);
            var appointments = new List<Appointment>
            {
                Appointment.Reconstitute(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    today,
                    "Pending",
                    patient: Patient.Reconstitute(Guid.NewGuid(), "Patient 1", DateTime.UtcNow, "Other", "000", "Addr"),
                    doctor: Doctor.Reconstitute(Guid.NewGuid(), "Dr. John Doe", "General")),
                Appointment.Reconstitute(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    today,
                    "Pending",
                    patient: Patient.Reconstitute(Guid.NewGuid(), "Patient 2", DateTime.UtcNow, "Other", "000", "Addr"),
                    doctor: Doctor.Reconstitute(Guid.NewGuid(), "Dr. Jane Smith", "General"))
            };

            _appointmentRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(appointments);

            var result = await _service.GetTodayNotificationsAsync("Doctor", "John Doe");

            Assert.NotNull(result);
            Assert.Equal(1, result.UnreadCount);
            Assert.Single(result.Notifications);
            Assert.Equal("Dr. John Doe", result.Notifications[0].DoctorName);
        }

        [Fact]
        public async Task GetTodayNotificationsAsync_ReturnsEmpty_WhenNoAppointments()
        {
            _appointmentRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Appointment>());

            var result = await _service.GetTodayNotificationsAsync("Admin", "admin");

            Assert.NotNull(result);
            Assert.Equal(0, result.UnreadCount);
            Assert.Empty(result.Notifications);
        }
    }
}
