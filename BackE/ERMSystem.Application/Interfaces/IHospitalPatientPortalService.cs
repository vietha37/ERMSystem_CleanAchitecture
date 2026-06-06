using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces
{
    public interface IHospitalPatientPortalService
    {
        Task<HospitalPatientPortalOverviewDto?> GetOverviewByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<HospitalPatientVisitHistoryResultDto?> GetVisitHistoryByUserIdAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
        Task<HospitalPaymentIntentDto?> CreateQrPaymentIntentAsync(
            Guid userId,
            Guid invoiceId,
            HospitalPatientPortalQrPaymentIntentRequestDto request,
            CancellationToken ct = default);
        Task<HospitalInvoiceDetailDto?> ConfirmQrPaymentAsync(
            Guid userId,
            ConfirmHospitalPaymentCallbackDto request,
            CancellationToken ct = default);
    }
}
