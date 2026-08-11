using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using ERMSystem.Domain.Entities;
using Moq;
using Xunit;

namespace ERMSystem.Tests.Services
{
    public class PrescriptionItemServiceTests
    {
        private readonly Mock<IPrescriptionItemRepository> _itemRepoMock;
        private readonly Mock<IPrescriptionRepository> _prescriptionRepoMock;
        private readonly PrescriptionItemService _service;

        public PrescriptionItemServiceTests()
        {
            _itemRepoMock = new Mock<IPrescriptionItemRepository>();
            _prescriptionRepoMock = new Mock<IPrescriptionRepository>();
            _service = new PrescriptionItemService(_itemRepoMock.Object, _prescriptionRepoMock.Object);
        }

        [Fact]
        public async Task AddItemToPrescriptionAsync_PrescriptionNotFound_ThrowsKeyNotFoundException()
        {
            var prescriptionId = Guid.NewGuid();
            _itemRepoMock.Setup(r => r.PrescriptionExistsAsync(prescriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new AddPrescriptionItemDto { MedicineId = Guid.NewGuid(), Dosage = "1 tab", Duration = "7 days" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddItemToPrescriptionAsync(prescriptionId, dto));
        }

        [Fact]
        public async Task AddItemToPrescriptionAsync_MedicineNotFound_ThrowsKeyNotFoundException()
        {
            var prescriptionId = Guid.NewGuid();
            var medicineId = Guid.NewGuid();
            _itemRepoMock.Setup(r => r.PrescriptionExistsAsync(prescriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _itemRepoMock.Setup(r => r.MedicineExistsAsync(medicineId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var dto = new AddPrescriptionItemDto { MedicineId = medicineId, Dosage = "1 tab", Duration = "7 days" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddItemToPrescriptionAsync(prescriptionId, dto));
        }

        [Fact]
        public async Task AddItemToPrescriptionAsync_Valid_AddsAndReturnsUpdatedPrescription()
        {
            var prescriptionId = Guid.NewGuid();
            var medicineId = Guid.NewGuid();
            var prescription = new Prescription
            {
                Id = prescriptionId,
                MedicalRecordId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                PrescriptionItems = new List<PrescriptionItem>()
            };

            _itemRepoMock.Setup(r => r.PrescriptionExistsAsync(prescriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _itemRepoMock.Setup(r => r.MedicineExistsAsync(medicineId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _prescriptionRepoMock.Setup(r => r.GetByIdAsync(prescriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(prescription);

            var dto = new AddPrescriptionItemDto { MedicineId = medicineId, Dosage = "2 tabs", Duration = "5 days" };
            var result = await _service.AddItemToPrescriptionAsync(prescriptionId, dto);

            Assert.NotNull(result);
            Assert.Equal(prescriptionId, result.Id);
            _itemRepoMock.Verify(r => r.AddAsync(It.IsAny<PrescriptionItem>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveItemFromPrescriptionAsync_ItemNotFound_ThrowsKeyNotFoundException()
        {
            var prescriptionId = Guid.NewGuid();
            _itemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((PrescriptionItem?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoveItemFromPrescriptionAsync(prescriptionId, Guid.NewGuid()));
        }

        [Fact]
        public async Task RemoveItemFromPrescriptionAsync_ItemBelongsToDifferentPrescription_ThrowsKeyNotFoundException()
        {
            var prescriptionId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var item = new PrescriptionItem { Id = itemId, PrescriptionId = Guid.NewGuid(), MedicineId = Guid.NewGuid(), Dosage = "X", Duration = "X" };
            _itemRepoMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoveItemFromPrescriptionAsync(prescriptionId, itemId));
        }

        [Fact]
        public async Task RemoveItemFromPrescriptionAsync_Valid_RemovesAndReturnsUpdatedPrescription()
        {
            var prescriptionId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var item = new PrescriptionItem { Id = itemId, PrescriptionId = prescriptionId, MedicineId = Guid.NewGuid(), Dosage = "X", Duration = "X" };
            var prescription = new Prescription { Id = prescriptionId, MedicalRecordId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, PrescriptionItems = new List<PrescriptionItem>() };

            _itemRepoMock.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            _prescriptionRepoMock.Setup(r => r.GetByIdAsync(prescriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(prescription);

            var result = await _service.RemoveItemFromPrescriptionAsync(prescriptionId, itemId);

            Assert.NotNull(result);
            _itemRepoMock.Verify(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
