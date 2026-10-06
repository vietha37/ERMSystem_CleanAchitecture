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

namespace ERMSystem.Tests.Services;

public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IHospitalIdentityBridgeService> _identityBridgeMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly AdminUserService _service;

    public AdminUserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _identityBridgeMock = new Mock<IHospitalIdentityBridgeService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _service = new AdminUserService(
            _userRepoMock.Object,
            _identityBridgeMock.Object,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task GetUsersAsync_WithInvalidRole_ThrowsArgumentException()
    {
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetUsersAsync(request, "SuperAdmin", CancellationToken.None));
    }

    [Fact]
    public async Task GetUsersAsync_WithValidRole_ReturnsPaginatedResult()
    {
        var users = new List<AppUser>
        {
            new() { Id = Guid.NewGuid(), Username = "doctor1", Name = "Dr. Strange", Role = AppRole.Doctor }
        };

        _userRepoMock.Setup(r => r.GetPagedAsync(1, 10, AppRole.Doctor, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 1));

        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetUsersAsync(request, AppRole.Doctor, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task CreateUserAsync_WhenUsernameExists_ThrowsInvalidOperationException()
    {
        var dto = new CreateAdminUserDto
        {
            Username = "existing_user",
            Name = "John Doe",
            Password = "Password123!",
            Role = AppRole.Doctor
        };

        _userRepoMock.Setup(r => r.UsernameExistsAsync("existing_user"))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateUserAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_HashesPasswordAndSyncs()
    {
        var dto = new CreateAdminUserDto
        {
            Username = "new_doctor",
            Name = "Dr. House",
            Password = "SecurePassword123!",
            Role = AppRole.Doctor
        };

        _userRepoMock.Setup(r => r.UsernameExistsAsync("new_doctor"))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword("SecurePassword123!"))
            .Returns("hashed_secret");

        var result = await _service.CreateUserAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new_doctor", result.Username);
        Assert.Equal("Dr. House", result.Name);
        Assert.Equal(AppRole.Doctor, result.Role);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<AppUser>(u =>
            u.Username == "new_doctor" &&
            u.PasswordHash == "hashed_secret")), Times.Once);

        _identityBridgeMock.Verify(b => b.SyncInternalUserAsync(
            It.Is<AppUser>(u => u.Username == "new_doctor"),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var dto = new UpdateAdminUserDto { Username = "newname", Name = "New Name" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateUserAsync(userId, dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNotDoctorOrCashier_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, Username = "admin", Role = AppRole.Admin };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var dto = new UpdateAdminUserDto { Username = "admin2", Name = "Admin Two" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateUserAsync(userId, dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenValidDoctor_DeletesAndDeactivates()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, Username = "doctor_del", Role = AppRole.Doctor };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.DeleteUserAsync(userId, CancellationToken.None);

        _userRepoMock.Verify(r => r.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _identityBridgeMock.Verify(b => b.DeactivateInternalUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }
}
