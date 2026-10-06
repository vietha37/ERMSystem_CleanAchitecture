using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Utilities;
using ERMSystem.Domain.Entities;
using Moq;
using Xunit;

namespace ERMSystem.Tests.Utilities;

public class DoctorAccessScopeHelperTests
{
    private readonly Mock<IHospitalDoctorWorklistRepository> _worklistRepoMock;

    public DoctorAccessScopeHelperTests()
    {
        _worklistRepoMock = new Mock<IHospitalDoctorWorklistRepository>();
    }

    [Theory]
    [InlineData(AppRole.Admin)]
    [InlineData(AppRole.Cashier)]
    public async Task ResolveScopedDoctorProfileIdAsync_NonDoctorRole_ReturnsNull(string role)
    {
        var result = await DoctorAccessScopeHelper.ResolveScopedDoctorProfileIdAsync(
            _worklistRepoMock.Object,
            role,
            "admin_user",
            CancellationToken.None);

        Assert.Null(result);
        _worklistRepoMock.Verify(r => r.ResolveDoctorByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveScopedDoctorProfileIdAsync_DoctorRole_ResolvesDoctorProfile()
    {
        var expectedDoctorProfileId = Guid.NewGuid();
        var doctorSnapshot = new HospitalDoctorProfileSnapshot
        {
            DoctorProfileId = expectedDoctorProfileId,
            DoctorName = "Dr. Kim",
            Username = "doctor_kim"
        };

        _worklistRepoMock.Setup(r => r.ResolveDoctorByUsernameAsync("doctor_kim", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctorSnapshot);

        var result = await DoctorAccessScopeHelper.ResolveScopedDoctorProfileIdAsync(
            _worklistRepoMock.Object,
            AppRole.Doctor,
            "doctor_kim",
            CancellationToken.None);

        Assert.Equal(expectedDoctorProfileId, result);
    }

    [Fact]
    public async Task CanAccessDoctorScopedDataAsync_NonDoctorRole_ReturnsTrue()
    {
        var canAccess = await DoctorAccessScopeHelper.CanAccessDoctorScopedDataAsync(
            _worklistRepoMock.Object,
            Guid.NewGuid(),
            AppRole.Admin,
            "admin",
            CancellationToken.None);

        Assert.True(canAccess);
    }

    [Fact]
    public async Task CanAccessDoctorScopedDataAsync_MatchingDoctor_ReturnsTrue()
    {
        var doctorProfileId = Guid.NewGuid();
        var doctorSnapshot = new HospitalDoctorProfileSnapshot
        {
            DoctorProfileId = doctorProfileId,
            DoctorName = "Dr. Smith",
            Username = "doctor_smith"
        };

        _worklistRepoMock.Setup(r => r.ResolveDoctorByUsernameAsync("doctor_smith", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctorSnapshot);

        var canAccess = await DoctorAccessScopeHelper.CanAccessDoctorScopedDataAsync(
            _worklistRepoMock.Object,
            doctorProfileId,
            AppRole.Doctor,
            "doctor_smith",
            CancellationToken.None);

        Assert.True(canAccess);
    }

    [Fact]
    public async Task CanAccessDoctorScopedDataAsync_DifferentDoctor_ReturnsFalse()
    {
        var ownDoctorProfileId = Guid.NewGuid();
        var otherDoctorProfileId = Guid.NewGuid();

        var doctorSnapshot = new HospitalDoctorProfileSnapshot
        {
            DoctorProfileId = ownDoctorProfileId,
            DoctorName = "Dr. Smith",
            Username = "doctor_smith"
        };

        _worklistRepoMock.Setup(r => r.ResolveDoctorByUsernameAsync("doctor_smith", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctorSnapshot);

        var canAccess = await DoctorAccessScopeHelper.CanAccessDoctorScopedDataAsync(
            _worklistRepoMock.Object,
            otherDoctorProfileId,
            AppRole.Doctor,
            "doctor_smith",
            CancellationToken.None);

        Assert.False(canAccess);
    }
}
