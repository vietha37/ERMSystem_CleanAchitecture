using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalAppointmentRepository : IHospitalAppointmentRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalAppointmentRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<HospitalBookingPatientSnapshot?> FindMatchingPatientAsync(
        string fullName,
        DateOnly dateOfBirth,
        string phone,
        string? email,
        CancellationToken ct = default)
    {
        var normalizedFullName = fullName.Trim();
        var normalizedPhone = phone.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        var patient = await _hospitalDbContext.Patients
            .AsNoTracking()
            .Where(x => x.DeletedAtUtc == null)
            .Where(x =>
                (x.Phone != null && x.Phone == normalizedPhone) ||
                (normalizedEmail != null && x.Email != null && x.Email == normalizedEmail) ||
                (x.FullName == normalizedFullName && x.DateOfBirth == dateOfBirth))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (patient == null)
        {
            return null;
        }

        return new HospitalBookingPatientSnapshot
        {
            PatientId = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Phone = patient.Phone,
            Email = patient.Email
        };
    }

    public Task<bool> HasDoctorConflictAsync(
        Guid doctorProfileId,
        DateTime appointmentStartUtc,
        DateTime appointmentEndUtc,
        Guid? excludingAppointmentId = null,
        CancellationToken ct = default)
    {
        return _hospitalDbContext.Appointments
            .AsNoTracking()
            .Where(x => x.DoctorProfileId == doctorProfileId)
            .Where(x => x.Status != "Cancelled")
            .Where(x => !excludingAppointmentId.HasValue || x.Id != excludingAppointmentId.Value)
            .AnyAsync(
                x => x.AppointmentStartUtc < appointmentEndUtc &&
                     (x.AppointmentEndUtc ?? x.AppointmentStartUtc) > appointmentStartUtc,
                ct);
    }

    public Task AddPatientAsync(HospitalBookingPatientCreateCommand patient, CancellationToken ct = default)
    {
        _hospitalDbContext.Patients.Add(new HospitalPatientEntity
        {
            Id = patient.PatientId,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            CreatedAtUtc = patient.CreatedAtUtc,
            UpdatedAtUtc = patient.UpdatedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddAppointmentAsync(HospitalBookingAppointmentCreateCommand appointment, CancellationToken ct = default)
    {
        _hospitalDbContext.Appointments.Add(new HospitalAppointmentEntity
        {
            Id = appointment.AppointmentId,
            AppointmentNumber = appointment.AppointmentNumber,
            PatientId = appointment.PatientId,
            DoctorProfileId = appointment.DoctorProfileId,
            ClinicId = appointment.ClinicId,
            AppointmentType = appointment.AppointmentType,
            BookingChannel = appointment.BookingChannel,
            Status = appointment.Status,
            AppointmentStartUtc = appointment.AppointmentStartUtc,
            AppointmentEndUtc = appointment.AppointmentEndUtc,
            ChiefComplaint = appointment.ChiefComplaint,
            Notes = appointment.Notes,
            CreatedAtUtc = appointment.CreatedAtUtc,
            UpdatedAtUtc = appointment.UpdatedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddOutboxMessageAsync(HospitalOutboxMessageCreateCommand message, CancellationToken ct = default)
    {
        _hospitalDbContext.OutboxMessages.Add(new HospitalOutboxMessageEntity
        {
            Id = message.OutboxMessageId,
            AggregateType = message.AggregateType,
            AggregateId = message.AggregateId,
            EventType = message.EventType,
            PayloadJson = message.PayloadJson,
            Status = message.Status,
            AvailableAtUtc = message.AvailableAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task<PaginatedResult<HospitalAppointmentWorklistItemDto>> GetWorklistAsync(
        HospitalAppointmentWorklistRequestDto request,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _hospitalDbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.CheckIn)
            .Include(x => x.QueueTickets)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var normalizedStatus = request.Status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (request.AppointmentDate.HasValue)
        {
            var (fromUtc, toUtc) = BuildUtcRange(request.AppointmentDate.Value);
            query = query.Where(x => x.AppointmentStartUtc >= fromUtc && x.AppointmentStartUtc < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.TextSearch))
        {
            var keyword = request.TextSearch.Trim();
            var pattern = $"%{keyword}%";
            query = query.Where(x =>
                EF.Functions.Like(x.AppointmentNumber, pattern) ||
                EF.Functions.Like(x.Patient.FullName, pattern) ||
                (x.Patient.Phone != null && EF.Functions.Like(x.Patient.Phone, pattern)) ||
                EF.Functions.Like(x.Patient.MedicalRecordNumber, pattern) ||
                EF.Functions.Like(x.DoctorProfile.StaffProfile.FullName, pattern) ||
                EF.Functions.Like(x.Clinic.Name, pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.AppointmentStartUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var mapped = items.Select(x => new HospitalAppointmentWorklistItemDto
        {
            AppointmentId = x.Id,
            AppointmentNumber = x.AppointmentNumber,
            PatientId = x.PatientId,
            PatientName = x.Patient.FullName,
            MedicalRecordNumber = x.Patient.MedicalRecordNumber,
            PatientPhone = x.Patient.Phone,
            DoctorProfileId = x.DoctorProfileId,
            DoctorName = x.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = x.DoctorProfile.Specialty.Name,
            ClinicName = x.Clinic.Name,
            FloorLabel = x.Clinic.FloorLabel,
            RoomLabel = x.Clinic.RoomLabel,
            AppointmentType = x.AppointmentType,
            BookingChannel = x.BookingChannel,
            Status = x.Status,
            AppointmentStartLocal = ConvertUtcToClinicLocal(x.AppointmentStartUtc),
            AppointmentEndLocal = x.AppointmentEndUtc.HasValue ? ConvertUtcToClinicLocal(x.AppointmentEndUtc.Value) : null,
            ChiefComplaint = x.ChiefComplaint,
            CounterLabel = x.CheckIn?.CounterLabel,
            QueueNumber = x.QueueTickets.OrderByDescending(q => q.Id).Select(q => q.QueueNumber).FirstOrDefault(),
            CheckInTimeLocal = x.CheckIn != null ? ConvertUtcToClinicLocal(x.CheckIn.CheckInTimeUtc) : null
        }).ToArray();

        return new PaginatedResult<HospitalAppointmentWorklistItemDto>(mapped, totalCount, pageNumber, pageSize);
    }

    public async Task<HospitalAppointmentAggregateSnapshot?> GetAppointmentAggregateAsync(
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.Appointments
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.CheckIn)
            .Include(x => x.QueueTickets)
            .FirstOrDefaultAsync(x => x.Id == appointmentId, ct);

        if (entity == null)
        {
            return null;
        }

        var latestQueue = entity.QueueTickets
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        return new HospitalAppointmentAggregateSnapshot
        {
            AppointmentId = entity.Id,
            AppointmentNumber = entity.AppointmentNumber,
            PatientId = entity.PatientId,
            PatientName = entity.Patient.FullName,
            MedicalRecordNumber = entity.Patient.MedicalRecordNumber,
            PatientPhone = entity.Patient.Phone,
            PatientEmail = entity.Patient.Email,
            DoctorProfileId = entity.DoctorProfileId,
            DoctorName = entity.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = entity.DoctorProfile.Specialty.Name,
            ClinicName = entity.Clinic.Name,
            FloorLabel = entity.Clinic.FloorLabel,
            RoomLabel = entity.Clinic.RoomLabel,
            AppointmentType = entity.AppointmentType,
            BookingChannel = entity.BookingChannel,
            Status = entity.Status,
            AppointmentStartUtc = entity.AppointmentStartUtc,
            AppointmentEndUtc = entity.AppointmentEndUtc,
            ChiefComplaint = entity.ChiefComplaint,
            CheckInId = entity.CheckIn?.Id,
            CheckInTimeUtc = entity.CheckIn?.CheckInTimeUtc,
            CounterLabel = entity.CheckIn?.CounterLabel,
            QueueNumber = latestQueue?.QueueNumber
        };
    }

    public async Task<int> GetNextQueueSequenceAsync(
        DateTime appointmentStartUtc,
        CancellationToken ct = default)
    {
        var localDate = ConvertUtcToClinicLocal(appointmentStartUtc).Date;
        var (fromUtc, toUtc) = BuildUtcRange(DateOnly.FromDateTime(localDate));

        var existingCount = await _hospitalDbContext.QueueTickets
            .AsNoTracking()
            .Where(x => x.Appointment.AppointmentStartUtc >= fromUtc && x.Appointment.AppointmentStartUtc < toUtc)
            .CountAsync(ct);

        return existingCount + 1;
    }

    public Task AddCheckInAsync(HospitalAppointmentCheckInCommand checkIn, CancellationToken ct = default)
    {
        _hospitalDbContext.CheckIns.Add(new HospitalCheckInEntity
        {
            Id = checkIn.CheckInId,
            AppointmentId = checkIn.AppointmentId,
            CheckInTimeUtc = checkIn.CheckInTimeUtc,
            CounterLabel = checkIn.CounterLabel,
            CheckInStatus = checkIn.CheckInStatus
        });

        return Task.CompletedTask;
    }

    public Task AddQueueTicketAsync(HospitalAppointmentQueueTicketCreateCommand ticket, CancellationToken ct = default)
    {
        _hospitalDbContext.QueueTickets.Add(new HospitalQueueTicketEntity
        {
            Id = ticket.QueueTicketId,
            AppointmentId = ticket.AppointmentId,
            QueueNumber = ticket.QueueNumber,
            QueueStatus = ticket.QueueStatus
        });

        return Task.CompletedTask;
    }

    public async Task UpdateStatusAsync(Guid appointmentId, string status, CancellationToken ct = default)
    {
        var appointment = await _hospitalDbContext.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId, ct);
        if (appointment == null)
        {
            throw new InvalidOperationException("Khong tim thay lich hen.");
        }

        appointment.Status = status;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
    }

    public async Task UpdateScheduleAsync(
        Guid appointmentId,
        Guid clinicId,
        DateTime appointmentStartUtc,
        DateTime appointmentEndUtc,
        string? notes,
        CancellationToken ct = default)
    {
        var appointment = await _hospitalDbContext.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId, ct);
        if (appointment == null)
        {
            throw new InvalidOperationException("Khong tim thay lich hen.");
        }

        appointment.ClinicId = clinicId;
        appointment.AppointmentStartUtc = appointmentStartUtc;
        appointment.AppointmentEndUtc = appointmentEndUtc;
        appointment.Notes = notes;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _hospitalDbContext.SaveChangesAsync(ct);

    private static TimeZoneInfo ResolveClinicTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), ResolveClinicTimeZone());

    private static DateTime ConvertLocalClinicTimeToUtc(DateTime localDateTime)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), ResolveClinicTimeZone());

    private static (DateTime FromUtc, DateTime ToUtc) BuildUtcRange(DateOnly localDate)
    {
        var fromLocal = localDate.ToDateTime(TimeOnly.MinValue);
        var toLocal = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return (ConvertLocalClinicTimeToUtc(fromLocal), ConvertLocalClinicTimeToUtc(toLocal));
    }
}
