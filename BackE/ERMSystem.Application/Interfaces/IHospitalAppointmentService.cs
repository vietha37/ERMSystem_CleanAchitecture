using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces
{
    public interface IHospitalAppointmentService
    {
        Task<HospitalAppointmentBookingResultDto> BookPublicAppointmentAsync(
            PublicHospitalAppointmentBookingRequestDto request,
            CancellationToken ct = default);

        Task<PaginatedResult<HospitalAppointmentWorklistItemDto>> GetWorklistAsync(
            HospitalAppointmentWorklistRequestDto request,
            string currentRole,
            string? currentUsername,
            CancellationToken ct = default);

        Task<HospitalAppointmentWorklistItemDto?> CheckInAsync(
            Guid appointmentId,
            HospitalAppointmentCheckInRequestDto request,
            CancellationToken ct = default);

        Task<HospitalAppointmentWorklistItemDto?> UpdateStatusAsync(
            Guid appointmentId,
            HospitalAppointmentStatusUpdateRequestDto request,
            CancellationToken ct = default);

        Task<HospitalAppointmentWorklistItemDto?> CancelAsync(
            Guid appointmentId,
            HospitalAppointmentCancelRequestDto request,
            CancellationToken ct = default);

        Task<HospitalAppointmentWorklistItemDto?> RescheduleAsync(
            Guid appointmentId,
            HospitalAppointmentRescheduleRequestDto request,
            CancellationToken ct = default);
    }
}
