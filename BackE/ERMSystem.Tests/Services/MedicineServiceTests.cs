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
    public class MedicineServiceTests
    {
        private readonly Mock<IMedicineRepository> _repoMock;
        private readonly MedicineService _service;

        public MedicineServiceTests()
        {
            _repoMock = new Mock<IMedicineRepository>();
            _service = new MedicineService(_repoMock.Object);
        }

        [Fact]
        public async Task GetAllMedicinesAsync_ReturnsPaginatedResult()
        {
            var medicines = new List<Medicine>
            {
                new Medicine { Id = Guid.NewGuid(), Name = "Paracetamol", Description = "Pain relief" }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<Medicine>)medicines, 1));

            var result = await _service.GetAllMedicinesAsync(new PaginationRequest { PageNumber = 1, PageSize = 10 });

            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetMedicineByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Medicine { Id = id, Name = "Aspirin", Description = "Blood thinner" });

            var result = await _service.GetMedicineByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Aspirin", result!.Name);
        }

        [Fact]
        public async Task GetMedicineByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Medicine?)null);

            var result = await _service.GetMedicineByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateMedicineAsync_ReturnsCreatedDto()
        {
            var dto = new CreateMedicineDto { Name = "Ibuprofen", Description = "Anti-inflammatory" };

            var result = await _service.CreateMedicineAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Ibuprofen", result.Name);
            Assert.NotEqual(Guid.Empty, result.Id);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Medicine>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMedicineAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Medicine?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateMedicineAsync(Guid.NewGuid(), new UpdateMedicineDto { Name = "X", Description = "Y" }));
        }

        [Fact]
        public async Task UpdateMedicineAsync_Found_UpdatesFields()
        {
            var id = Guid.NewGuid();
            var medicine = new Medicine { Id = id, Name = "Old", Description = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(medicine);

            await _service.UpdateMedicineAsync(id, new UpdateMedicineDto { Name = "New", Description = "New" });

            Assert.Equal("New", medicine.Name);
            _repoMock.Verify(r => r.UpdateAsync(medicine, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMedicineAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Medicine?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteMedicineAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteMedicineAsync_Found_Deletes()
        {
            var id = Guid.NewGuid();
            var medicine = new Medicine { Id = id, Name = "Del", Description = "Del" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(medicine);

            await _service.DeleteMedicineAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(medicine, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
