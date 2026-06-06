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
        private readonly IHospitalBillingService _billingService;

        public HospitalPatientPortalService(
            IHospitalPatientPortalRepository repository,
            IHospitalBillingService billingService)
        {
            _repository = repository;
            _billingService = billingService;
        }

        public Task<HospitalPatientPortalOverviewDto?> GetOverviewByUserIdAsync(Guid userId, CancellationToken ct = default)
            => _repository.GetOverviewByUserIdAsync(userId, ct);

        public Task<HospitalPatientVisitHistoryResultDto?> GetVisitHistoryByUserIdAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            => _repository.GetVisitHistoryByUserIdAsync(userId, pageNumber, pageSize, ct);

        public async Task<HospitalPaymentIntentDto?> CreateQrPaymentIntentAsync(
            Guid userId,
            Guid invoiceId,
            HospitalPatientPortalQrPaymentIntentRequestDto request,
            CancellationToken ct = default)
        {
            var access = await _repository.GetInvoicePaymentAccessAsync(userId, invoiceId, ct);
            if (access == null)
            {
                return null;
            }

            if (access.InvoiceStatus == "Paid" || access.BalanceAmount <= 0)
            {
                throw new InvalidOperationException("Hoa don nay da thanh toan du, khong can tao giao dich QR moi.");
            }

            if (access.InvoiceStatus == "Cancelled")
            {
                throw new InvalidOperationException("Hoa don da huy, khong the thanh toan QR.");
            }

            var amount = request.Amount.GetValueOrDefault(access.BalanceAmount);
            if (amount <= 0)
            {
                amount = access.BalanceAmount;
            }

            return await _billingService.CreatePaymentIntentAsync(
                invoiceId,
                new CreateHospitalPaymentIntentDto
                {
                    GatewayProvider = string.IsNullOrWhiteSpace(request.GatewayProvider)
                        ? "MockGateway"
                        : request.GatewayProvider.Trim(),
                    PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
                        ? "QR"
                        : request.PaymentMethod.Trim(),
                    Amount = amount
                },
                userId,
                "patient-portal",
                ct);
        }

        public async Task<HospitalInvoiceDetailDto?> ConfirmQrPaymentAsync(
            Guid userId,
            ConfirmHospitalPaymentCallbackDto request,
            CancellationToken ct = default)
        {
            var access = await _repository.GetInvoicePaymentAccessAsync(userId, request.InvoiceId, ct);
            if (access == null)
            {
                return null;
            }

            request.GatewayProvider = string.IsNullOrWhiteSpace(request.GatewayProvider)
                ? "MockGateway"
                : request.GatewayProvider.Trim();
            request.GatewayEventId = string.IsNullOrWhiteSpace(request.GatewayEventId)
                ? $"PORTAL-{Guid.NewGuid():N}"
                : request.GatewayEventId.Trim();
            request.GatewayTimestampUtc ??= DateTime.UtcNow;

            return await _billingService.ConfirmPaymentCallbackAsync(
                request,
                userId,
                "patient-portal",
                isSimulation: true,
                callbackSource: "patient-portal-qr",
                ct);
        }
    }
}
