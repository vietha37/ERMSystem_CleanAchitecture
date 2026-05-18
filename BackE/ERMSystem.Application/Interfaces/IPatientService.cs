using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces
{
    public interface IPatientService
    {
        Task<PaginatedResult<PatientDto>> GetAllPatientsAsync(PaginationRequest request, CancellationToken ct = default);
        Task<PatientDto?> GetPatientByIdAsync(Guid id, CancellationToken ct = default);
        Task<PatientDto?> GetPatientByAppUserIdAsync(Guid appUserId, CancellationToken ct = default);
        Task<IReadOnlyCollection<PotentialDuplicatePatientDto>> GetPotentialDuplicatesAsync(Guid patientId, CancellationToken ct = default);
        Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto, CancellationToken ct = default);
        Task UpdatePatientAsync(Guid id, UpdatePatientDto updatePatientDto, CancellationToken ct = default);
        Task<MergePatientsResultDto> MergePatientsAsync(
            MergePatientsRequestDto request,
            Guid? actorUserId,
            string? actorUsername,
            CancellationToken ct = default);
        Task DeletePatientAsync(Guid id, CancellationToken ct = default);
    }
}
