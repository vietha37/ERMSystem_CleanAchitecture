using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces;

public interface IHospitalBillingService
{
    Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default);

    Task<HospitalInvoiceDetailDto?> GetByIdAsync(Guid invoiceId, string currentRole, string? currentUsername, CancellationToken ct = default);
    Task<HospitalBillingEligibleEncounterDto[]> GetEligibleEncountersAsync(string currentRole, string? currentUsername, CancellationToken ct = default);
    Task<HospitalBillingEncounterSnapshot?> GetEncounterPreviewAsync(Guid encounterId, CancellationToken ct = default);
    Task<HospitalInvoiceDetailDto> CreateInvoiceAsync(CreateHospitalInvoiceDto request, CancellationToken ct = default);
    Task<HospitalPaymentIntentDto?> CreatePaymentIntentAsync(Guid invoiceId, CreateHospitalPaymentIntentDto request, Guid? actorUserId, string? actorUsername, CancellationToken ct = default);
    Task<HospitalInvoiceDetailDto?> ReceivePaymentAsync(Guid invoiceId, ReceiveHospitalPaymentDto request, Guid? actorUserId, string? actorUsername, CancellationToken ct = default);
    Task<HospitalInvoiceDetailDto?> ConfirmPaymentCallbackAsync(
        ConfirmHospitalPaymentCallbackDto request,
        Guid? actorUserId,
        string? actorUsername,
        bool isSimulation,
        string? callbackSource,
        CancellationToken ct = default);
    Task<HospitalInvoiceDetailDto?> RefundPaymentAsync(Guid invoiceId, RefundHospitalPaymentDto request, Guid? actorUserId, string? actorUsername, CancellationToken ct = default);
    Task<HospitalPaymentReconciliationSummaryDto> GetReconciliationSummaryAsync(string currentRole, string? currentUsername, CancellationToken ct = default);
    Task<HospitalPaymentReconciliationPreviewDto> PreviewReconciliationAsync(
        HospitalPaymentReconciliationPreviewRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default);
    Task<HospitalPaymentReconciliationApplyResultDto> ApplyReconciliationAsync(
        HospitalPaymentReconciliationApplyRequestDto request,
        Guid? actorUserId,
        string? actorUsername,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default);
}
