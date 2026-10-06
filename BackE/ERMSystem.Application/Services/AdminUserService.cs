using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services;

/// <summary>
/// Triển khai dịch vụ quản lý tài khoản người dùng cho quản trị viên.
/// Tiếp nhận trách nhiệm nghiệp vụ được tách ra từ AdminUsersController.
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
    private readonly IPasswordHasher _passwordHasher;

    public AdminUserService(
        IUserRepository userRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        _passwordHasher = passwordHasher;
    }

    public async Task<PaginatedResult<AdminUserDto>> GetUsersAsync(
        PaginationRequest request,
        string? role,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(role) &&
            !string.Equals(role, AppRole.Doctor, StringComparison.Ordinal) &&
            !string.Equals(role, AppRole.Cashier, StringComparison.Ordinal) &&
            !string.Equals(role, AppRole.Patient, StringComparison.Ordinal))
        {
            throw new ArgumentException("Role filter must be Doctor, Cashier or Patient.");
        }

        var (items, totalCount) = await _userRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            role,
            request.TextSearch,
            ct);

        var mapped = items
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Name = u.Name,
                Role = u.Role
            });

        return new PaginatedResult<AdminUserDto>(
            mapped,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<AdminUserDto> CreateUserAsync(
        CreateAdminUserDto dto,
        CancellationToken ct = default)
    {
        if (await _userRepository.UsernameExistsAsync(dto.Username))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = dto.Username.Trim(),
            Name = dto.Name.Trim(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Role = dto.Role
        };

        await _userRepository.AddAsync(user);
        await _hospitalIdentityBridgeService.SyncInternalUserAsync(user, ct: ct);

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role
        };
    }

    public async Task UpdateUserAsync(
        Guid id,
        UpdateAdminUserDto dto,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        if (user.Role != AppRole.Doctor && user.Role != AppRole.Cashier)
        {
            throw new InvalidOperationException("Only Doctor and Cashier accounts can be updated here.");
        }

        if (await _userRepository.UsernameExistsAsync(dto.Username, id, ct))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        var previousUsername = user.Username;
        user.Username = dto.Username.Trim();
        user.Name = dto.Name.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            user.Role = dto.Role;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(dto.Password);
        }

        await _userRepository.UpdateAsync(user, ct);
        await _hospitalIdentityBridgeService.SyncInternalUserAsync(user, previousUsername, ct);
    }

    public async Task DeleteUserAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        if (user.Role != AppRole.Doctor && user.Role != AppRole.Cashier && user.Role != AppRole.Patient)
        {
            throw new InvalidOperationException("Only Doctor, Cashier and Patient accounts can be deleted here.");
        }

        await _userRepository.DeleteAsync(user, ct);
        await _hospitalIdentityBridgeService.DeactivateInternalUserAsync(user, ct);
    }

    public async Task<HospitalInternalUserSyncResultDto> SyncHospitalIdentityAsync(
        CancellationToken ct = default)
    {
        var internalUsers = await _userRepository.GetInternalUsersAsync(ct);
        return await _hospitalIdentityBridgeService.SyncInternalUsersAsync(internalUsers, ct);
    }
}
