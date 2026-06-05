using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly HospitalDbContext _context;

        public UserRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            var normalizedUsername = username.Trim();

            return await BuildUserQuery()
                .FirstOrDefaultAsync(x => x.Username == normalizedUsername);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            var normalizedUsername = username.Trim();

            return await _context.Users
                .AnyAsync(u => u.Username == normalizedUsername && u.DeletedAtUtc == null);
        }

        public async Task AddAsync(AppUser user)
        {
            var nowUtc = DateTime.UtcNow;
            var entity = new HospitalUserEntity
            {
                Id = user.Id,
                Username = user.Username.Trim(),
                PasswordHash = user.PasswordHash,
                PrimaryRoleCode = user.Role,
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            await _context.Users.AddAsync(entity);
            await EnsureUserRoleAsync(entity.Id, user.Role, nowUtc, CancellationToken.None);
            await _context.SaveChangesAsync();
        }

        public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await BuildUserQuery()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<bool> UsernameExistsAsync(string username, Guid excludeId, CancellationToken ct = default)
        {
            var normalizedUsername = username.Trim();

            return await _context.Users.AnyAsync(
                u => u.Username == normalizedUsername && u.Id != excludeId && u.DeletedAtUtc == null,
                ct);
        }

        public async Task<(IEnumerable<AppUser> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? role = null,
            string? textSearch = null,
            CancellationToken ct = default)
        {
            var query = BuildUserQuery();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }
            else
            {
                query = query.Where(u => u.Role == AppRole.Doctor || u.Role == AppRole.Cashier);
            }

            if (!string.IsNullOrWhiteSpace(textSearch))
            {
                var keyword = textSearch.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(keyword) ||
                    u.Name.ToLower().Contains(keyword) ||
                    u.Role.ToLower().Contains(keyword));
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<IReadOnlyList<AppUser>> GetInternalUsersAsync(CancellationToken ct = default)
        {
            return await BuildUserQuery()
                .Where(u => u.Role == AppRole.Admin || u.Role == AppRole.Doctor || u.Role == AppRole.Cashier)
                .OrderBy(u => u.Username)
                .ToListAsync(ct);
        }

        public async Task UpdateAsync(AppUser user, CancellationToken ct = default)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.Username = user.Username.Trim();
            entity.PasswordHash = user.PasswordHash;
            entity.PrimaryRoleCode = user.Role;
            entity.IsActive = true;
            entity.DeletedAtUtc = null;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await EnsureUserRoleAsync(entity.Id, user.Role, entity.UpdatedAtUtc, ct);
            await SyncDisplayNameAsync(user, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(AppUser user, CancellationToken ct = default)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.IsActive = false;
            entity.DeletedAtUtc ??= DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        private IQueryable<AppUser> BuildUserQuery()
        {
            return _context.Users
                .AsNoTracking()
                .Where(u => u.DeletedAtUtc == null)
                .Select(u => new AppUser
                {
                    Id = u.Id,
                    Username = u.Username,
                    Name =
                        _context.StaffProfiles
                            .Where(sp => sp.UserId == u.Id)
                            .Select(sp => sp.FullName)
                            .FirstOrDefault()
                        ?? _context.PatientAccounts
                            .Where(pa => pa.UserId == u.Id)
                            .Select(pa => pa.Patient.FullName)
                            .FirstOrDefault()
                        ?? u.Username,
                    PasswordHash = u.PasswordHash,
                    Role = u.PrimaryRoleCode
                });
        }

        private async Task EnsureUserRoleAsync(Guid userId, string roleCode, DateTime grantedAtUtc, CancellationToken ct)
        {
            var hasRole = await _context.UserRoles
                .AnyAsync(x => x.UserId == userId && x.RoleCode == roleCode, ct);

            if (hasRole)
            {
                return;
            }

            _context.UserRoles.Add(new HospitalUserRoleEntity
            {
                UserId = userId,
                RoleCode = roleCode,
                GrantedAtUtc = grantedAtUtc,
                GrantedByUserId = null
            });
        }

        private async Task SyncDisplayNameAsync(AppUser user, CancellationToken ct)
        {
            var normalizedName = string.IsNullOrWhiteSpace(user.Name)
                ? user.Username.Trim()
                : user.Name.Trim();

            var staffProfile = await _context.StaffProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id, ct);

            if (staffProfile != null)
            {
                staffProfile.FullName = normalizedName;
            }

            var patientAccount = await _context.PatientAccounts
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.UserId == user.Id, ct);

            if (patientAccount?.Patient != null)
            {
                patientAccount.Patient.FullName = normalizedName;
                patientAccount.Patient.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
