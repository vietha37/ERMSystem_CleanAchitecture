using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;

namespace ERMSystem.Application.Interfaces
{
    public interface IHospitalAppointmentRepository
    {
        Task<HospitalBookingPatientSnapshot?> FindMatchingPatientAsync(
            string fullName,
            DateOnly dateOfBirth,
            string phone,
            string? email,
            CancellationToken ct = default);

        Task<bool> HasDoctorConflictAsync(
            Guid doctorProfileId,
            DateTime appointmentStartUtc,
            DateTime appointmentEndUtc,
            Guid? excludingAppointmentId = null,
            CancellationToken ct = default);

        Task AddPatientAsync(HospitalBookingPatientCreateCommand patient, CancellationToken ct = default);
        Task AddAppointmentAsync(HospitalBookingAppointmentCreateCommand appointment, CancellationToken ct = default);
        Task AddOutboxMessageAsync(HospitalOutboxMessageCreateCommand message, CancellationToken ct = default);
        Task<PaginatedResult<HospitalAppointmentWorklistItemDto>> GetWorklistAsync(
            HospitalAppointmentWorklistRequestDto request,
            CancellationToken ct = default);
        Task<HospitalAppointmentAggregateSnapshot?> GetAppointmentAggregateAsync(
            Guid appointmentId,
            CancellationToken ct = default);
        Task<int> GetNextQueueSequenceAsync(
            DateTime appointmentStartUtc,
            CancellationToken ct = default);
        Task AddCheckInAsync(HospitalAppointmentCheckInCommand checkIn, CancellationToken ct = default);
        Task AddQueueTicketAsync(HospitalAppointmentQueueTicketCreateCommand ticket, CancellationToken ct = default);
        Task UpdateStatusAsync(Guid appointmentId, string status, CancellationToken ct = default);
        Task UpdateScheduleAsync(
            Guid appointmentId,
            Guid clinicId,
            DateTime appointmentStartUtc,
            DateTime appointmentEndUtc,
            string? notes,
            CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }

    public class HospitalBookingPatientSnapshot
    {
        public Guid PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class HospitalBookingPatientCreateCommand
    {
        public Guid PatientId { get; set; }
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class HospitalBookingAppointmentCreateCommand
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public Guid DoctorProfileId { get; set; }
        public Guid ClinicId { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
        public string BookingChannel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppointmentStartUtc { get; set; }
        public DateTime AppointmentEndUtc { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class HospitalOutboxMessageCreateCommand
    {
        public Guid OutboxMessageId { get; set; }
        public string AggregateType { get; set; } = string.Empty;
        public Guid AggregateId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AvailableAtUtc { get; set; }
    }

    public class HospitalAppointmentAggregateSnapshot
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public Guid DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string? FloorLabel { get; set; }
        public string? RoomLabel { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
        public string BookingChannel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppointmentStartUtc { get; set; }
        public DateTime? AppointmentEndUtc { get; set; }
        public string? ChiefComplaint { get; set; }
        public Guid? CheckInId { get; set; }
        public DateTime? CheckInTimeUtc { get; set; }
        public string? CounterLabel { get; set; }
        public string? QueueNumber { get; set; }
    }

    public class HospitalAppointmentCheckInCommand
    {
        public Guid CheckInId { get; set; }
        public Guid AppointmentId { get; set; }
        public DateTime CheckInTimeUtc { get; set; }
        public string? CounterLabel { get; set; }
        public string CheckInStatus { get; set; } = string.Empty;
    }

    public class HospitalAppointmentQueueTicketCreateCommand
    {
        public Guid QueueTicketId { get; set; }
        public Guid AppointmentId { get; set; }
        public string QueueNumber { get; set; } = string.Empty;
        public string QueueStatus { get; set; } = string.Empty;
    }
}
