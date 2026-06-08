using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalCatalogRepository : IHospitalCatalogRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalCatalogRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<IReadOnlyList<HospitalDepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        return await _hospitalDbContext.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new HospitalDepartmentDto
            {
                Id = x.Id,
                DepartmentCode = x.DepartmentCode,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<HospitalSpecialtyDto>> GetSpecialtiesAsync(CancellationToken ct = default)
    {
        return await _hospitalDbContext.Specialties
            .AsNoTracking()
            .Include(x => x.Department)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new HospitalSpecialtyDto
            {
                Id = x.Id,
                SpecialtyCode = x.SpecialtyCode,
                Name = x.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<HospitalClinicDto>> GetClinicsAsync(CancellationToken ct = default)
    {
        return await _hospitalDbContext.Clinics
            .AsNoTracking()
            .Include(x => x.Department)
            .OrderBy(x => x.Name)
            .Select(x => new HospitalClinicDto
            {
                Id = x.Id,
                ClinicCode = x.ClinicCode,
                Name = x.Name,
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                FloorLabel = x.FloorLabel,
                RoomLabel = x.RoomLabel,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<HospitalServiceCatalogDto>> GetServicesAsync(CancellationToken ct = default)
    {
        return await _hospitalDbContext.ServiceCatalog
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => new HospitalServiceCatalogDto
            {
                Id = x.Id,
                ServiceCode = x.ServiceCode,
                Name = x.Name,
                Category = x.Category,
                UnitPrice = x.UnitPrice,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }
}
