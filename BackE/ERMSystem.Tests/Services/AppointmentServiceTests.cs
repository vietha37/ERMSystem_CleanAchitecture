using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using ERMSystem.Domain.Entities;
using Moq;
using Xunit;

namespace ERMSystem.Tests.Services
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IAppointmentRepository> _repoMock;
        private readonly Mock<IDashboardQueryCache> _cacheMock;
        private readonly AppointmentService _service;

        public AppointmentServiceTests()
        {
            _repoMock = new Mock<IAppointmentRepository>();
            _cacheMock = new Mock<IDashboardQueryCache>();
            _service = new AppointmentService(_repoMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetAllAppointmentsAsync_ReturnsPaginatedResult()
        {
            var appointments = new List<Appointment>
            {
                new Appointment { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<Appointment>)appointments, 1));

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };
            var result = await _service.GetAllAppointmentsAsync(request);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            var appointment = new Appointment { Id = id, PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

            var result = await _service.GetAppointmentByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
            Assert.Equal("Pending", result.Status);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);

            var result = await _service.GetAppointmentByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAppointmentAsync_ValidData_ReturnsDto()
        {
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            _repoMock.Setup(r => r.PatientExistsAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.DoctorExistsAsync(doctorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var dto = new CreateAppointmentDto { PatientId = patientId, DoctorId = doctorId, AppointmentDate = DateTime.UtcNow.AddDays(1), Status = "Pending" };
            var result = await _service.CreateAppointmentAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("Pending", result.Status);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAppointmentAsync_InvalidStatus_ThrowsArgumentException()
        {
            var dto = new CreateAppointmentDto { PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "InvalidStatus" };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAppointmentAsync(dto));
        }

        [Fact]
        public async Task CreateAppointmentAsync_PatientNotFound_ThrowsKeyNotFoundException()
        {
            var patientId = Guid.NewGuid();
            _repoMock.Setup(r => r.PatientExistsAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new CreateAppointmentDto { PatientId = patientId, DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAppointmentAsync(dto));
        }

        [Fact]
        public async Task CreateAppointmentAsync_DoctorNotFound_ThrowsKeyNotFoundException()
        {
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            _repoMock.Setup(r => r.PatientExistsAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.DoctorExistsAsync(doctorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new CreateAppointmentDto { PatientId = patientId, DoctorId = doctorId, AppointmentDate = DateTime.UtcNow, Status = "Pending" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAppointmentAsync(dto));
        }

        [Fact]
        public async Task UpdateAppointmentAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);

            var dto = new UpdateAppointmentDto { PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAppointmentAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAppointmentAsync_InvalidStatus_ThrowsArgumentException()
        {
            var id = Guid.NewGuid();
            var appointment = new Appointment { Id = id, PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

            var dto = new UpdateAppointmentDto { PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "BadStatus" };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAppointmentAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAppointmentAsync_ValidData_UpdatesSuccessfully()
        {
            var id = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var appointment = new Appointment { Id = id, PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), AppointmentDate = DateTime.UtcNow, Status = "Pending" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
            _repoMock.Setup(r => r.PatientExistsAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.DoctorExistsAsync(doctorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var dto = new UpdateAppointmentDto { PatientId = patientId, DoctorId = doctorId, AppointmentDate = DateTime.UtcNow.AddDays(2), Status = "Completed" };

            await _service.UpdateAppointmentAsync(id, dto);

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAppointmentAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAppointmentAsync_Found_DeletesAndInvalidatesCache()
        {
            var id = Guid.NewGuid();
            var appointment = new Appointment { Id = id, Status = "Pending" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);

            await _service.DeleteAppointmentAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Completed")]
        [InlineData("Cancelled")]
        public async Task CreateAppointmentAsync_AllValidStatuses_Succeed(string status)
        {
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            _repoMock.Setup(r => r.PatientExistsAsync(patientId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.DoctorExistsAsync(doctorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var dto = new CreateAppointmentDto { PatientId = patientId, DoctorId = doctorId, AppointmentDate = DateTime.UtcNow, Status = status };
            var result = await _service.CreateAppointmentAsync(dto);

            Assert.Equal(status, result.Status);
        }
    }
}
