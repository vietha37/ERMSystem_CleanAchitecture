using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalEncounterService
{
    Task<PaginatedResult<HospitalEncounterSummaryDto>> GetWorklistAsync(
        HospitalEncounterWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> GetByIdAsync(Guid encounterId, string currentRole, string? currentUsername, CancellationToken ct = default);

    Task<HospitalEncounterEligibleAppointmentDto[]> GetEligibleAppointmentsAsync(string currentRole, string? currentUsername, CancellationToken ct = default);

    Task<HospitalEncounterDetailDto> CreateAsync(
        CreateHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> UpdateAsync(
        Guid encounterId,
        UpdateHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> ApproveAsync(
        Guid encounterId,
        ApproveHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> SignAsync(
        Guid encounterId,
        SignHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> AddAttachmentAsync(
        Guid encounterId,
        AddHospitalEncounterAttachmentDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalEncounterDetailDto?> UploadAttachmentAsync(
        Guid encounterId,
        string documentType,
        string fileName,
        string? contentType,
        long contentLength,
        Stream content,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default);

    Task<HospitalStoredAttachmentContentDto?> GetAttachmentContentAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default);

    Task<HospitalEncounterAttachmentDownloadTicketDto?> CreateAttachmentDownloadTicketAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default);

    Task<HospitalStoredAttachmentContentDto?> GetAttachmentContentByTicketAsync(
        string accessToken,
        CancellationToken ct = default);
}
