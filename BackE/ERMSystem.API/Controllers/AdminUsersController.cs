using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

/// <summary>
/// Controller tiếp nhận yêu cầu quản lý người dùng từ tài khoản Quản trị viên (Admin).
/// Đã được tái cấu trúc tuân thủ Clean Architecture: chỉ điều phối HTTP request/response,
/// toàn bộ logic nghiệp vụ, bảo mật và truy xuất dữ liệu được ủy quyền cho IAdminUserService.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>
    /// Lấy danh sách người dùng phân trang và lọc theo vai trò.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.AdminUsers.Read)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] PaginationRequest request,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        try
        {
            var result = await _adminUserService.GetUsersAsync(request, role, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Tạo người dùng mới trong hệ thống.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPermissions.AdminUsers.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _adminUserService.CreateUserAsync(dto, ct);
            return CreatedAtAction(nameof(GetUsers), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Cập nhật thông tin tài khoản nhân viên y tế.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPermissions.AdminUsers.Update)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAdminUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _adminUserService.UpdateUserAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("taken", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Xóa tài khoản người dùng khỏi hệ thống.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPermissions.AdminUsers.Delete)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        try
        {
            await _adminUserService.DeleteUserAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Đồng bộ danh bạ người dùng nội bộ sang hệ thống bệnh viện.
    /// </summary>
    [HttpPost("sync-hospital-identity")]
    [Authorize(Policy = AppPermissions.AdminUsers.SyncIdentity)]
    public async Task<IActionResult> SyncHospitalIdentity(CancellationToken ct)
    {
        var result = await _adminUserService.SyncHospitalIdentityAsync(ct);
        return Ok(result);
    }
}
