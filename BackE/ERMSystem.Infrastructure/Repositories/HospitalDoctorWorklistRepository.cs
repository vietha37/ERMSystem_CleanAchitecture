using System;
using System.Linq;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalDoctorWorklistRepository : IHospitalDoctorWorklistRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalDoctorWorklistRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public Task<HospitalDoctorProfileSnapshot?> GetDoctorProfileAsync(Guid doctorProfileId, CancellationToken ct = default)
    {
        return _hospitalDbContext.DoctorProfiles
            .AsNoTracking()
            .Where(x => x.Id == doctorProfileId)
            .Select(x => new HospitalDoctorProfileSnapshot
            {
                DoctorProfileId = x.Id,
                DoctorName = x.StaffProfile.FullName,
                SpecialtyName = x.Specialty.Name,
                Username = x.StaffProfile.User.Username
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<HospitalDoctorProfileSnapshot?> ResolveDoctorByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalizedUsername = username.Trim();

        return _hospitalDbContext.DoctorProfiles
            .AsNoTracking()
            .Where(x => x.StaffProfile.User.Username == normalizedUsername)
            .Select(x => new HospitalDoctorProfileSnapshot
            {
                DoctorProfileId = x.Id,
                DoctorName = x.StaffProfile.FullName,
                SpecialtyName = x.Specialty.Name,
                Username = x.StaffProfile.User.Username
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<HospitalDoctorWorklistSnapshot[]> GetWorklistAsync(DateOnly workDate, Guid? doctorProfileId, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = BuildUtcRange(workDate);

        var appointmentsQuery = _hospitalDbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Where(x => x.Status != "Cancelled")
            .Where(x => x.AppointmentStartUtc >= fromUtc && x.AppointmentStartUtc < toUtc);

        if (doctorProfileId.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(x => x.DoctorProfileId == doctorProfileId.Value);
        }

        var appointments = await appointmentsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.AppointmentStartUtc)
            .ToListAsync(ct);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var encounters = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Diagnoses)
            .Where(x => x.AppointmentId != null && appointmentIds.Contains(x.AppointmentId.Value))
            .ToListAsync(ct);

        var encounterLookup = encounters
            .Where(x => x.AppointmentId.HasValue)
            .ToDictionary(x => x.AppointmentId!.Value, x => x);

        var encounterIds = encounters.Select(x => x.Id).ToArray();
        var prescriptions = await _hospitalDbContext.Prescriptions
            .AsNoTracking()
            .Where(x => encounterIds.Contains(x.OrderHeader.EncounterId))
            .Select(x => new
            {
                x.Id,
                x.PrescriptionNumber,
                EncounterId = x.OrderHeader.EncounterId
            })
            .ToListAsync(ct);

        var prescriptionLookup = prescriptions.ToDictionary(x => x.EncounterId, x => x);

        return appointments.Select(x =>
        {
            encounterLookup.TryGetValue(x.Id, out var encounter);
            var diagnosis = encounter?.Diagnoses
                .OrderByDescending(d => d.IsPrimary)
                .ThenByDescending(d => d.NotedAtUtc)
                .FirstOrDefault();
            var prescription = encounter != null && prescriptionLookup.TryGetValue(encounter.Id, out var foundPrescription)
                ? foundPrescription
                : null;

            return new HospitalDoctorWorklistSnapshot
            {
                AppointmentId = x.Id,
                AppointmentNumber = x.AppointmentNumber,
                AppointmentStatus = x.Status,
                AppointmentStartUtc = x.AppointmentStartUtc,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                MedicalRecordNumber = x.Patient.MedicalRecordNumber,
                DoctorProfileId = x.DoctorProfileId,
                DoctorName = x.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = x.DoctorProfile.Specialty.Name,
                ClinicName = x.Clinic.Name,
                EncounterId = encounter?.Id,
                EncounterNumber = encounter?.EncounterNumber,
                EncounterStatus = encounter?.EncounterStatus,
                PrimaryDiagnosisName = diagnosis?.DiagnosisName,
                PrescriptionId = prescription?.Id,
                PrescriptionNumber = prescription?.PrescriptionNumber
            };
        }).ToArray();
    }

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

    private static DateTime ConvertLocalClinicTimeToUtc(DateTime localDateTime)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), ResolveClinicTimeZone());

    private static (DateTime FromUtc, DateTime ToUtc) BuildUtcRange(DateOnly localDate)
    {
        var fromLocal = localDate.ToDateTime(TimeOnly.MinValue);
        var toLocal = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return (ConvertLocalClinicTimeToUtc(fromLocal), ConvertLocalClinicTimeToUtc(toLocal));
    }
}
