using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services
{
    public class HospitalCatalogService : IHospitalCatalogService
    {
        private readonly IHospitalCatalogRepository _hospitalCatalogRepository;
        private readonly ICatalogQueryCache _cache;

        public HospitalCatalogService(
            IHospitalCatalogRepository hospitalCatalogRepository,
            ICatalogQueryCache cache)
        {
            _hospitalCatalogRepository = hospitalCatalogRepository;
            _cache = cache;
        }

        public async Task<HospitalCatalogOverviewDto> GetOverviewAsync(CancellationToken ct = default)
        {
            var cached = await _cache.GetOverviewAsync(ct);
            if (cached != null)
            {
                return cached;
            }

            var departments = await _hospitalCatalogRepository.GetDepartmentsAsync(ct);
            var specialties = await _hospitalCatalogRepository.GetSpecialtiesAsync(ct);
            var clinics = await _hospitalCatalogRepository.GetClinicsAsync(ct);
            var services = await _hospitalCatalogRepository.GetServicesAsync(ct);

            var overview = new HospitalCatalogOverviewDto
            {
                Departments = departments,
                Specialties = specialties,
                Clinics = clinics,
                Services = services
            };

            await _cache.SetOverviewAsync(overview, ct);
            return overview;
        }

        public Task<IReadOnlyList<HospitalDepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default)
            => _hospitalCatalogRepository.GetDepartmentsAsync(ct);

        public Task<IReadOnlyList<HospitalSpecialtyDto>> GetSpecialtiesAsync(CancellationToken ct = default)
            => _hospitalCatalogRepository.GetSpecialtiesAsync(ct);

        public Task<IReadOnlyList<HospitalClinicDto>> GetClinicsAsync(CancellationToken ct = default)
            => _hospitalCatalogRepository.GetClinicsAsync(ct);

        public Task<IReadOnlyList<HospitalServiceCatalogDto>> GetServicesAsync(CancellationToken ct = default)
            => _hospitalCatalogRepository.GetServicesAsync(ct);
    }
}
