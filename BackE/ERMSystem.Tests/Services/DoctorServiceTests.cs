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
    public class DoctorServiceTests
    {
        private readonly Mock<IDoctorRepository> _repoMock;
        private readonly DoctorService _service;

        public DoctorServiceTests()
        {
            _repoMock = new Mock<IDoctorRepository>();
            _service = new DoctorService(_repoMock.Object);
        }

        [Fact]
        public async Task GetAllDoctorsAsync_ReturnsPaginatedResult()
        {
            var doctors = new List<Doctor>
            {
                new Doctor { Id = Guid.NewGuid(), FullName = "Dr. A", Specialty = "Cardiology" },
                new Doctor { Id = Guid.NewGuid(), FullName = "Dr. B", Specialty = "Neurology" }
            };
            _repoMock.Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(((IEnumerable<Doctor>)doctors, 2));

            var result = await _service.GetAllDoctorsAsync(new PaginationRequest { PageNumber = 1, PageSize = 10 });

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetDoctorByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Doctor { Id = id, FullName = "Dr. Test", Specialty = "Surgery" });

            var result = await _service.GetDoctorByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Dr. Test", result!.FullName);
            Assert.Equal("Surgery", result.Specialty);
        }

        [Fact]
        public async Task GetDoctorByIdAsync_NotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

            var result = await _service.GetDoctorByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateDoctorAsync_ReturnsCreatedDto()
        {
            var dto = new CreateDoctorDto { FullName = "Dr. New", Specialty = "Dermatology" };

            var result = await _service.CreateDoctorAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Dr. New", result.FullName);
            Assert.Equal("Dermatology", result.Specialty);
            Assert.NotEqual(Guid.Empty, result.Id);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Doctor>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateDoctorAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateDoctorAsync(Guid.NewGuid(), new UpdateDoctorDto { FullName = "X", Specialty = "Y" }));
        }

        [Fact]
        public async Task UpdateDoctorAsync_Found_UpdatesFields()
        {
            var id = Guid.NewGuid();
            var doctor = new Doctor { Id = id, FullName = "Old", Specialty = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

            await _service.UpdateDoctorAsync(id, new UpdateDoctorDto { FullName = "New", Specialty = "New" });

            Assert.Equal("New", doctor.FullName);
            Assert.Equal("New", doctor.Specialty);
            _repoMock.Verify(r => r.UpdateAsync(doctor, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteDoctorAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteDoctorAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteDoctorAsync_Found_DeletesDoctor()
        {
            var id = Guid.NewGuid();
            var doctor = new Doctor { Id = id, FullName = "Del", Specialty = "Del" };
            _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);

            await _service.DeleteDoctorAsync(id);

            _repoMock.Verify(r => r.DeleteAsync(doctor, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
