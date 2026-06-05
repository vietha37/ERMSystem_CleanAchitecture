using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using ERMSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;

        public AdminUsersController(
            IUserRepository userRepository,
            IHospitalIdentityBridgeService hospitalIdentityBridgeService)
        {
            _userRepository = userRepository;
            _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AdminUsers.Read)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] PaginationRequest request,
            [FromQuery] string? role,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(role) &&
                !string.Equals(role, AppRole.Doctor, StringComparison.Ordinal) &&
                !string.Equals(role, AppRole.Cashier, StringComparison.Ordinal))
            {
                return BadRequest("Role filter must be Doctor or Cashier.");
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

            var result = new PaginatedResult<AdminUserDto>(
                mapped,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.AdminUsers.Create)]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _userRepository.UsernameExistsAsync(dto.Username))
            {
                return Conflict($"Username '{dto.Username}' is already taken.");
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = dto.Username.Trim(),
                Name = dto.Name.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            await _userRepository.AddAsync(user);
            await _hospitalIdentityBridgeService.SyncInternalUserAsync(user, ct: ct);

            var created = new AdminUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role
            };

            return CreatedAtAction(nameof(GetUsers), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = AppPermissions.AdminUsers.Update)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAdminUserDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByIdAsync(id, ct);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            if (user.Role != AppRole.Doctor && user.Role != AppRole.Cashier)
            {
                return BadRequest("Only Doctor and Cashier accounts can be updated here.");
            }

            if (await _userRepository.UsernameExistsAsync(dto.Username, id, ct))
            {
                return Conflict($"Username '{dto.Username}' is already taken.");
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
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _userRepository.UpdateAsync(user, ct);
            await _hospitalIdentityBridgeService.SyncInternalUserAsync(user, previousUsername, ct);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = AppPermissions.AdminUsers.Delete)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(id, ct);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            if (user.Role != AppRole.Doctor && user.Role != AppRole.Cashier)
            {
                return BadRequest("Only Doctor and Cashier accounts can be deleted here.");
            }

            await _userRepository.DeleteAsync(user, ct);
            await _hospitalIdentityBridgeService.DeactivateInternalUserAsync(user, ct);
            return NoContent();
        }

        [HttpPost("sync-hospital-identity")]
        [Authorize(Policy = AppPermissions.AdminUsers.SyncIdentity)]
        public async Task<IActionResult> SyncHospitalIdentity(CancellationToken ct)
        {
            var internalUsers = await _userRepository.GetInternalUsersAsync(ct);
            var result = await _hospitalIdentityBridgeService.SyncInternalUsersAsync(internalUsers, ct);
            return Ok(result);
        }
    }
}
