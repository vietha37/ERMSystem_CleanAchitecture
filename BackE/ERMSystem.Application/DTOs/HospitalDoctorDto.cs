using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMSystem.Application.DTOs
{
    public class HospitalDoctorDto
    {
        public Guid DoctorProfileId { get; set; }
        public Guid StaffProfileId { get; set; }
        public Guid SpecialtyId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Biography { get; set; }
        public int? YearsOfExperience { get; set; }
        public decimal? ConsultationFee { get; set; }
        public bool IsBookable { get; set; }
        public IReadOnlyList<HospitalDoctorScheduleDto> Schedules { get; set; } = Array.Empty<HospitalDoctorScheduleDto>();
    }

    public class HospitalDoctorScheduleDto
    {
        public Guid ScheduleId { get; set; }
        public Guid ClinicId { get; set; }
        public byte DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int SlotMinutes { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string? FloorLabel { get; set; }
        public string? RoomLabel { get; set; }
    }

    public class PublicHospitalAppointmentBookingRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Email { get; set; }

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public Guid DoctorProfileId { get; set; }

        public Guid? SpecialtyId { get; set; }

        [MaxLength(50)]
        public string? ServiceCode { get; set; }

        [Required]
        public DateOnly PreferredDate { get; set; }

        [Required]
        public TimeOnly PreferredTime { get; set; }

        [MaxLength(1000)]
        public string? ChiefComplaint { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class HospitalAppointmentBookingResultDto
    {
        public Guid AppointmentId { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public Guid DoctorProfileId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public DateTime AppointmentStartLocal { get; set; }
        public DateTime AppointmentEndLocal { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsExistingPatient { get; set; }
        public bool NotificationQueued { get; set; }
    }
}
