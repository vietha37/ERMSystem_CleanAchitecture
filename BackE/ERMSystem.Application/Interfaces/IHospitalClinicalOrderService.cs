using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalClinicalOrderService
{
    Task<PaginatedResult<HospitalClinicalOrderSummaryDto>> GetWorklistAsync(
        HospitalClinicalOrderWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default);

    Task<HospitalClinicalOrderDetailDto?> GetByIdAsync(Guid clinicalOrderId, string currentRole, string? currentUsername, CancellationToken ct = default);
    Task<HospitalClinicalOrderEligibleEncounterDto[]> GetEligibleEncountersAsync(string currentRole, string? currentUsername, CancellationToken ct = default);
    Task<HospitalClinicalOrderCatalogItemDto[]> GetCatalogAsync(CancellationToken ct = default);
    Task<HospitalClinicalOrderDetailDto> CreateAsync(
        CreateHospitalClinicalOrderDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);
    Task<HospitalClinicalOrderDetailDto?> RecordLabResultAsync(
        Guid clinicalOrderId,
        RecordHospitalLabResultDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);
    Task<HospitalClinicalOrderDetailDto?> RecordImagingReportAsync(
        Guid clinicalOrderId,
        RecordHospitalImagingReportDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);
}
