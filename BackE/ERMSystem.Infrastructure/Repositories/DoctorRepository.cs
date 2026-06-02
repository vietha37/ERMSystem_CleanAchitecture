using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly HospitalDbContext _context;

        public DoctorRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Doctor>> GetAllAsync(CancellationToken ct = default)
        {
            var doctors = await BuildDoctorQuery().ToListAsync(ct);
            return doctors.Select(HospitalLegacyRepositoryMapper.MapDoctor).ToList();
        }

        public async Task<(IEnumerable<Doctor> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = BuildDoctorQuery();
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(x => x.StaffProfile.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.Select(HospitalLegacyRepositoryMapper.MapDoctor).ToList(), totalCount);
        }

        public async Task<Doctor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var doctor = await BuildDoctorQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
            return doctor == null ? null : HospitalLegacyRepositoryMapper.MapDoctor(doctor);
        }

        public async Task AddAsync(Doctor doctor, CancellationToken ct = default)
        {
            var specialty = await FindOrCreateSpecialtyAsync(doctor.Specialty, ct);
            var departmentId = specialty.DepartmentId ?? await GetFallbackDepartmentIdAsync(ct);
            var nowUtc = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            var username = await GenerateUniqueUsernameAsync("doctor", ct);
            var user = new HospitalUserEntity
            {
                Id = userId,
                Username = username,
                PasswordHash = "LEGACY-NOLOGIN",
                PrimaryRoleCode = AppRole.Doctor,
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            var staffProfile = new HospitalStaffProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StaffCode = $"BS-{doctor.Id.ToString("N")[..8].ToUpperInvariant()}",
                FullName = doctor.FullName.Trim(),
                DepartmentId = departmentId,
                IsActive = true,
                CreatedAtUtc = nowUtc
            };

            var doctorProfile = new HospitalDoctorProfileEntity
            {
                Id = doctor.Id,
                StaffProfileId = staffProfile.Id,
                SpecialtyId = specialty.Id,
                IsBookable = true
            };

            await _context.Users.AddAsync(user, ct);
            await _context.UserRoles.AddAsync(new HospitalUserRoleEntity
            {
                UserId = userId,
                RoleCode = AppRole.Doctor,
                GrantedAtUtc = nowUtc
            }, ct);
            await _context.StaffProfiles.AddAsync(staffProfile, ct);
            await _context.DoctorProfiles.AddAsync(doctorProfile, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Doctor doctor, CancellationToken ct = default)
        {
            var entity = await _context.DoctorProfiles
                .Include(x => x.StaffProfile)
                .Include(x => x.Specialty)
                .FirstOrDefaultAsync(x => x.Id == doctor.Id, ct);

            if (entity == null)
            {
                return;
            }

            entity.StaffProfile.FullName = doctor.FullName.Trim();
            var specialty = await FindOrCreateSpecialtyAsync(doctor.Specialty, ct);
            entity.SpecialtyId = specialty.Id;
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Doctor doctor, CancellationToken ct = default)
        {
            var entity = await _context.DoctorProfiles
                .Include(x => x.StaffProfile)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == doctor.Id, ct);

            if (entity == null)
            {
                return;
            }

            entity.IsBookable = false;
            entity.StaffProfile.IsActive = false;
            entity.StaffProfile.User.IsActive = false;
            entity.StaffProfile.User.DeletedAtUtc ??= DateTime.UtcNow;
            entity.StaffProfile.User.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        private IQueryable<HospitalDoctorProfileEntity> BuildDoctorQuery()
        {
            return _context.DoctorProfiles
                .AsNoTracking()
                .Include(x => x.StaffProfile)
                .Include(x => x.Specialty)
                .Where(x => x.StaffProfile.IsActive);
        }

        private async Task<HospitalSpecialtyEntity> FindOrCreateSpecialtyAsync(string specialtyName, CancellationToken ct)
        {
            var normalizedName = specialtyName.Trim();
            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(x => x.Name == normalizedName, ct);

            if (specialty != null)
            {
                return specialty;
            }

            specialty = new HospitalSpecialtyEntity
            {
                Id = Guid.NewGuid(),
                SpecialtyCode = $"SP-{normalizedName.ToUpperInvariant().Replace(" ", "-")}",
                Name = normalizedName,
                DepartmentId = await GetFallbackDepartmentIdAsync(ct),
                IsActive = true
            };

            await _context.Specialties.AddAsync(specialty, ct);
            return specialty;
        }

        private async Task<Guid?> GetFallbackDepartmentIdAsync(CancellationToken ct)
        {
            return await _context.Departments
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string prefix, CancellationToken ct)
        {
            var counter = await _context.Users.CountAsync(x => x.Username.StartsWith(prefix), ct);
            string candidate;

            do
            {
                candidate = $"{prefix}{counter:D2}";
                counter++;
            } while (await _context.Users.AnyAsync(x => x.Username == candidate, ct));

            return candidate;
        }
    }
}
