using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ nghiệp vụ quản lý người dùng hệ thống dành cho Quản trị viên (Admin).
/// Tách rời trách nhiệm nghiệp vụ khỏi Controller để tuân thủ Clean Architecture.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Lấy danh sách người dùng có phân trang và lọc theo vai trò (Doctor, Cashier, Patient).
    /// </summary>
    Task<PaginatedResult<AdminUserDto>> GetUsersAsync(
        PaginationRequest request,
        string? role,
        CancellationToken ct = default);

    /// <summary>
    /// Tạo người dùng mới (băm mật khẩu, lưu CSDL và đồng bộ sang hồ sơ bệnh viện).
    /// </summary>
    Task<AdminUserDto> CreateUserAsync(
        CreateAdminUserDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật thông tin tài khoản nhân viên y tế (bác sĩ / thu ngân).
    /// </summary>
    Task UpdateUserAsync(
        Guid id,
        UpdateAdminUserDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa tài khoản người dùng và vô hiệu hóa trong hệ thống bệnh viện.
    /// </summary>
    Task DeleteUserAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Đồng bộ toàn bộ tài khoản nội bộ (Admin, Doctor, Cashier) vào danh bạ bệnh viện.
    /// </summary>
    Task<HospitalInternalUserSyncResultDto> SyncHospitalIdentityAsync(
        CancellationToken ct = default);
}
