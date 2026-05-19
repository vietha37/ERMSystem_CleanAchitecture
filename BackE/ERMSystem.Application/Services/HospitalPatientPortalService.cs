using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services
{
    public class HospitalPatientPortalService : IHospitalPatientPortalService
    {
        private readonly IHospitalPatientPortalRepository _repository;

        public HospitalPatientPortalService(IHospitalPatientPortalRepository repository)
        {
            _repository = repository;
        }

        public Task<HospitalPatientPortalOverviewDto?> GetOverviewByUserIdAsync(Guid userId, CancellationToken ct = default)
            => _repository.GetOverviewByUserIdAsync(userId, ct);

        public Task<HospitalPatientVisitHistoryResultDto?> GetVisitHistoryByUserIdAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            => _repository.GetVisitHistoryByUserIdAsync(userId, pageNumber, pageSize, ct);
    }
}
