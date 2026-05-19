using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces
{
    public interface IHospitalNotificationDeliveryService
    {
        Task<NotificationDeliveryListDto> GetDeliveriesAsync(
            string? status,
            string? channelCode,
            string? recipient,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
        Task<NotificationDeliverySummaryDto> GetSummaryAsync(CancellationToken ct = default);
        Task<NotificationDeliveryRetryResult> RetryDeliveryAsync(
            Guid deliveryId,
            Guid? actorUserId,
            string? actorUsername,
            CancellationToken ct = default);
    }
}
