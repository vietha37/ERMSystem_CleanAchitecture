using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services
{
    public class HospitalNotificationDeliveryService : IHospitalNotificationDeliveryService
    {
        private readonly IHospitalNotificationDeliveryRepository _repository;
        private readonly IComplianceAuditRecorder _complianceAuditRecorder;

        public HospitalNotificationDeliveryService(
            IHospitalNotificationDeliveryRepository repository,
            IComplianceAuditRecorder complianceAuditRecorder)
        {
            _repository = repository;
            _complianceAuditRecorder = complianceAuditRecorder;
        }

        public Task<NotificationDeliveryListDto> GetDeliveriesAsync(
            string? status,
            string? channelCode,
            string? recipient,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
            => _repository.GetDeliveriesAsync(status, channelCode, recipient, pageNumber, pageSize, ct);

        public Task<NotificationDeliverySummaryDto> GetSummaryAsync(CancellationToken ct = default)
            => _repository.GetSummaryAsync(ct);

        public async Task<NotificationDeliveryRetryResult> RetryDeliveryAsync(
            Guid deliveryId,
            Guid? actorUserId,
            string? actorUsername,
            CancellationToken ct = default)
        {
            var result = await _repository.RetryDeliveryAsync(deliveryId, ct);
            if (result == NotificationDeliveryRetryResult.Requeued)
            {
                await _complianceAuditRecorder.RecordAsync(
                    actorUserId,
                    actorUsername ?? "unknown",
                    "NotificationDeliveryRetried",
                    "Info",
                    $"DeliveryId={deliveryId}; Action=RetryQueued.",
                    ct);
            }

            return result;
        }
    }
}
