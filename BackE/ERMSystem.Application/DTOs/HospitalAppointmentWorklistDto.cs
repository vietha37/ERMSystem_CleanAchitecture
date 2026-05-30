using System;
using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class HospitalAppointmentWorklistRequestDto
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        [MaxLength(30)]
        public string? Status { get; set; }

        public DateOnly? AppointmentDate { get; set; }

        [MaxLength(200)]
        public string? TextSearch { get; set; }

        public Guid? DoctorProfileId { get; set; }
    }

    public class HospitalAppointmentWorklistItemDto
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }
        public Guid DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string? FloorLabel { get; set; }
        public string? RoomLabel { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
        public string BookingChannel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppointmentStartLocal { get; set; }
        public DateTime? AppointmentEndLocal { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? CounterLabel { get; set; }
        public string? QueueNumber { get; set; }
        public DateTime? CheckInTimeLocal { get; set; }
    }

    public class HospitalAppointmentStatusUpdateRequestDto
    {
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;
    }

    public class HospitalAppointmentCancelRequestDto
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class HospitalAppointmentRescheduleRequestDto
    {
        [Required]
        public DateOnly PreferredDate { get; set; }

        [Required]
        public TimeOnly PreferredTime { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class HospitalAppointmentCheckInRequestDto
    {
        [MaxLength(50)]
        public string? CounterLabel { get; set; }
    }
}
