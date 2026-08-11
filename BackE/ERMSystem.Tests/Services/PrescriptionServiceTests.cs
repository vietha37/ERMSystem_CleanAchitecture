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
    public class PrescriptionServiceTests
    {
        private readonly Mock<IPrescriptionRepository> _repoMock;
        private readonly Mock<IDashboardQueryCache> _cacheMock;
        private readonly PrescriptionService _service;

        public PrescriptionServiceTests()
        {
            _repoMock = new Mock<IPrescriptionRepository>();
            _cacheMock = new Mock<IDashboardQueryCache>();
            _service = new PrescriptionService(_repoMock.Object, _cacheMock.Object);
        }

        [Fact]
        public async Task GetAllPrescriptionsAsync_ReturnsPaginatedResult()
        {
            var prescriptions = new List<Prescription>
            {
                new Prescription { Id = Guid.NewGuid(), MedicalRecordId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<Prescription>)prescriptions, 1));

            var result = await _service.GetAllPrescriptionsAsync(new PaginationRequest { PageNumber = 1, PageSize = 10 });

            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetPrescriptionByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Prescription { Id = id, MedicalRecordId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });

            var result = await _service.GetPrescriptionByIdAsync(id);
            Assert.NotNull(result);
            Assert.Equal(id, result!.Id);
        }

        [Fact]
        public async Task GetPrescriptionByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Prescription?)null);
            Assert.Null(await _service.GetPrescriptionByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetPrescriptionByMedicalRecordIdAsync_Found_ReturnsDto()
        {
            var mrId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByMedicalRecordIdAsync(mrId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Prescription { Id = Guid.NewGuid(), MedicalRecordId = mrId, CreatedAt = DateTime.UtcNow });

            var result = await _service.GetPrescriptionByMedicalRecordIdAsync(mrId);
            Assert.NotNull(result);
            Assert.Equal(mrId, result!.MedicalRecordId);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_MedicalRecordNotFound_ThrowsKeyNotFoundException()
        {
            var mrId = Guid.NewGuid();
            _repoMock.Setup(r => r.MedicalRecordExistsAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreatePrescriptionAsync(new CreatePrescriptionDto { MedicalRecordId = mrId }));
        }

        [Fact]
        public async Task CreatePrescriptionAsync_DuplicateForMedicalRecord_ThrowsInvalidOperationException()
        {
            var mrId = Guid.NewGuid();
            _repoMock.Setup(r => r.MedicalRecordExistsAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.PrescriptionExistsForMedicalRecordAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreatePrescriptionAsync(new CreatePrescriptionDto { MedicalRecordId = mrId }));
        }

        [Fact]
        public async Task CreatePrescriptionAsync_Valid_CreatesSuccessfully()
        {
            var mrId = Guid.NewGuid();
            _repoMock.Setup(r => r.MedicalRecordExistsAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _repoMock.Setup(r => r.PrescriptionExistsForMedicalRecordAsync(mrId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await _service.CreatePrescriptionAsync(new CreatePrescriptionDto { MedicalRecordId = mrId });

            Assert.NotNull(result);
            Assert.Equal(mrId, result.MedicalRecordId);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Prescription>(), It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePrescriptionAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Prescription?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeletePrescriptionAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeletePrescriptionAsync_Found_Deletes()
        {
            var id = Guid.NewGuid();
            var prescription = new Prescription { Id = id, MedicalRecordId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(prescription);

            await _service.DeletePrescriptionAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(prescription, It.IsAny<CancellationToken>()), Times.Once);
            _cacheMock.Verify(c => c.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
