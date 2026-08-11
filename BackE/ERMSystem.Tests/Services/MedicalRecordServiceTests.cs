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
    public class MedicalRecordServiceTests
    {
        private readonly Mock<IMedicalRecordRepository> _repoMock;
        private readonly Mock<IDashboardQueryCache> _cacheMock;
        private readonly MedicalRecordService _service;

        public MedicalRecordServiceTests()
        {
            _repoMock = new Mock<IMedicalRecordRepository>();
            _cacheMock = new Mock<IDashboardQueryCache>();
            _service = new MedicalRecordService(_repoMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetAllMedicalRecordsAsync_ReturnsPaginatedResult()
        {
            var records = new List<MedicalRecord>
            {
                new MedicalRecord { Id = Guid.NewGuid(), AppointmentId = Guid.NewGuid(), Symptoms = "Fever", Diagnosis = "Flu", Notes = "Rest" }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<MedicalRecord>)records, 1));

            var result = await _service.GetAllMedicalRecordsAsync(new PaginationRequest { PageNumber = 1, PageSize = 10 });

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetMedicalRecordByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MedicalRecord { Id = id, AppointmentId = Guid.NewGuid(), Symptoms = "S", Diagnosis = "D", Notes = "N" });

            var result = await _service.GetMedicalRecordByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
        }

        [Fact]
        public async Task GetMedicalRecordByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MedicalRecord?)null);
            Assert.Null(await _service.GetMedicalRecordByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetMedicalRecordByAppointmentIdAsync_Found_ReturnsDto()
        {
            var appointmentId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByAppointmentIdAsync(appointmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MedicalRecord { Id = Guid.NewGuid(), AppointmentId = appointmentId, Symptoms = "S", Diagnosis = "D", Notes = "N" });

            var result = await _service.GetMedicalRecordByAppointmentIdAsync(appointmentId);

            Assert.NotNull(result);
            Assert.Equal(appointmentId, result!.AppointmentId);
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_AppointmentNotFound_ThrowsKeyNotFoundException()
        {
            var appointmentId = Guid.NewGuid();
            _repoMock.Setup(r => r.AppointmentExistsAsync(appointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new CreateMedicalRecordDto { AppointmentId = appointmentId, Symptoms = "S", Diagnosis = "D" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateMedicalRecordAsync(dto));
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_DuplicateForAppointment_ThrowsInvalidOperationException()
        {
            var appointmentId = Guid.NewGuid();
            _repoMock.Setup(r => r.AppointmentExistsAsync(appointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.MedicalRecordExistsForAppointmentAsync(appointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var dto = new CreateMedicalRecordDto { AppointmentId = appointmentId, Symptoms = "S", Diagnosis = "D" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateMedicalRecordAsync(dto));
        }

        [Fact]
        public async Task CreateMedicalRecordAsync_Valid_CreatesSuccessfully()
        {
            var appointmentId = Guid.NewGuid();
            _repoMock.Setup(r => r.AppointmentExistsAsync(appointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.MedicalRecordExistsForAppointmentAsync(appointmentId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new CreateMedicalRecordDto { AppointmentId = appointmentId, Symptoms = "Headache", Diagnosis = "Migraine", Notes = "Painkiller" };
            var result = await _service.CreateMedicalRecordAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Headache", result.Symptoms);
            Assert.Equal("Migraine", result.Diagnosis);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<MedicalRecord>(), It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMedicalRecordAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MedicalRecord?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateMedicalRecordAsync(Guid.NewGuid(), new UpdateMedicalRecordDto { Symptoms = "S", Diagnosis = "D" }));
        }

        [Fact]
        public async Task UpdateMedicalRecordAsync_Found_UpdatesFields()
        {
            var id = Guid.NewGuid();
            var record = new MedicalRecord { Id = id, Symptoms = "Old", Diagnosis = "Old", Notes = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

            await _service.UpdateMedicalRecordAsync(id, new UpdateMedicalRecordDto { Symptoms = "New", Diagnosis = "New", Notes = "New" });

            Assert.Equal("New", record.Symptoms);
            _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMedicalRecordAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MedicalRecord?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteMedicalRecordAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteMedicalRecordAsync_Found_Deletes()
        {
            var id = Guid.NewGuid();
            var record = new MedicalRecord { Id = id, Symptoms = "S", Diagnosis = "D", Notes = "N" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(record);

            await _service.DeleteMedicalRecordAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
