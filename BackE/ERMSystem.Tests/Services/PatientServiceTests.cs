using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PatientServiceTests
    {
        private readonly Mock<IPatientRepository> _repoMock;
        private readonly Mock<IComplianceAuditRecorder> _auditMock;
        private readonly Mock<IDashboardQueryCache> _cacheMock;
        private readonly PatientService _service;

        public PatientServiceTests()
        {
            _repoMock = new Mock<IPatientRepository>();
            _auditMock = new Mock<IComplianceAuditRecorder>();
            _cacheMock = new Mock<IDashboardQueryCache>();
            _service = new PatientService(_repoMock.Object, _auditMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetAllPatientsAsync_ReturnsPaginatedResult()
        {
            var patients = new List<Patient>
            {
                new Patient { Id = Guid.NewGuid(), FullName = "Nguyen Van A", DateOfBirth = new DateTime(1990, 1, 1), Gender = "Male", Phone = "0901234567", Address = "HCM" }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<Patient>)patients, 1));

            var result = await _service.GetAllPatientsAsync(new PaginationRequest { PageNumber = 1, PageSize = 10 });

            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetPatientByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Patient { Id = id, FullName = "Test", DateOfBirth = DateTime.UtcNow, Gender = "Male", Phone = "123", Address = "Addr" });

            var result = await _service.GetPatientByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Test", result!.FullName);
        }

        [Fact]
        public async Task GetPatientByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            var result = await _service.GetPatientByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPatientByAppUserIdAsync_Found_ReturnsDto()
        {
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByAppUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Patient { Id = Guid.NewGuid(), AppUserId = userId, FullName = "User", DateOfBirth = DateTime.UtcNow, Gender = "Male", Phone = "123", Address = "Addr" });

            var result = await _service.GetPatientByAppUserIdAsync(userId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreatePatientAsync_ReturnsCreatedDto()
        {
            var dto = new CreatePatientDto
            {
                FullName = "New Patient",
                DateOfBirth = new DateTime(2000, 5, 15),
                Gender = "Female",
                Phone = "0987654321",
                Address = "Hanoi"
            };

            var result = await _service.CreatePatientAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("New Patient", result.FullName);
            Assert.NotEqual(Guid.Empty, result.Id);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePatientAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdatePatientAsync(Guid.NewGuid(), new UpdatePatientDto { FullName = "X", Gender = "Male", Phone = "1", Address = "A" }));
        }

        [Fact]
        public async Task UpdatePatientAsync_Found_UpdatesFields()
        {
            var id = Guid.NewGuid();
            var patient = new Patient { Id = id, FullName = "Old", DateOfBirth = DateTime.UtcNow, Gender = "Male", Phone = "0", Address = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

            await _service.UpdatePatientAsync(id, new UpdatePatientDto { FullName = "New", DateOfBirth = new DateTime(1995, 1, 1), Gender = "Female", Phone = "111", Address = "New" });

            Assert.Equal("New", patient.FullName);
            Assert.Equal("Female", patient.Gender);
            _repoMock.Verify(r => r.UpdateAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePatientAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeletePatientAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeletePatientAsync_Found_Deletes()
        {
            var id = Guid.NewGuid();
            var patient = new Patient { Id = id, FullName = "Del", DateOfBirth = DateTime.UtcNow, Gender = "Male", Phone = "0", Address = "X" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

            await _service.DeletePatientAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(patient, It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MergePatientsAsync_SameIds_ThrowsInvalidOperationException()
        {
            var id = Guid.NewGuid();
            var dto = new MergePatientsRequestDto { SourcePatientId = id, TargetPatientId = id };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.MergePatientsAsync(dto, Guid.NewGuid(), "admin"));
        }

        [Fact]
        public async Task MergePatientsAsync_SourceNotFound_ThrowsKeyNotFoundException()
        {
            var dto = new MergePatientsRequestDto { SourcePatientId = Guid.NewGuid(), TargetPatientId = Guid.NewGuid() };
            _repoMock.Setup(r => r.GetByIdAsync(dto.SourcePatientId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.MergePatientsAsync(dto, Guid.NewGuid(), "admin"));
        }

        [Fact]
        public async Task MergePatientsAsync_TargetNotFound_ThrowsKeyNotFoundException()
        {
            var sourceId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Patient { Id = sourceId, FullName = "S", DateOfBirth = DateTime.UtcNow, Gender = "M", Phone = "1", Address = "A" });
            _repoMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            var dto = new MergePatientsRequestDto { SourcePatientId = sourceId, TargetPatientId = targetId };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.MergePatientsAsync(dto, Guid.NewGuid(), "admin"));
        }

        [Fact]
        public async Task MergePatientsAsync_ValidMerge_ReturnsResult()
        {
            var sourceId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var source = new Patient { Id = sourceId, FullName = "Source", DateOfBirth = DateTime.UtcNow, Gender = "M", Phone = "1", Address = "A", AppUserId = Guid.NewGuid() };
            var target = new Patient { Id = targetId, FullName = "Target", DateOfBirth = DateTime.UtcNow, Gender = "F", Phone = "2", Address = "B", AppUserId = null };
            _repoMock.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync(source);
            _repoMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>())).ReturnsAsync(target);
            _repoMock.Setup(r => r.ReassignAppointmentsAsync(sourceId, targetId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

            var dto = new MergePatientsRequestDto { SourcePatientId = sourceId, TargetPatientId = targetId };
            var result = await _service.MergePatientsAsync(dto, Guid.NewGuid(), "admin");

            Assert.Equal(sourceId, result.SourcePatientId);
            Assert.Equal(targetId, result.TargetPatientId);
            Assert.Equal(3, result.ReassignedAppointmentCount);
            Assert.True(result.AppUserLinkMoved);
            _repoMock.Verify(r => r.MergeAsync(source, target, It.IsAny<CancellationToken>()), Times.Once);
            _auditMock.Verify(a => a.RecordAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPotentialDuplicatesAsync_PatientNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetPotentialDuplicatesAsync(Guid.NewGuid()));
        }
    }
}
