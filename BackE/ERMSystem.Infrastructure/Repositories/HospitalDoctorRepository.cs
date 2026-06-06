using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalDoctorRepository : IHospitalDoctorRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalDoctorRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<IReadOnlyList<HospitalDoctorDto>> GetDoctorsAsync(Guid? specialtyId = null, CancellationToken ct = default)
    {
        var query = _hospitalDbContext.DoctorProfiles
            .AsNoTracking()
            .Include(x => x.StaffProfile)
                .ThenInclude(x => x.Department)
            .Include(x => x.Specialty)
            .Include(x => x.DoctorSchedules)
                .ThenInclude(x => x.Clinic)
            .Where(x => x.IsBookable);

        if (specialtyId.HasValue)
        {
            query = query.Where(x => x.SpecialtyId == specialtyId.Value);
        }

        var doctors = await query
            .OrderBy(x => x.StaffProfile.FullName)
            .ToListAsync(ct);

        return doctors.Select(MapDoctor).ToList();
    }

    public async Task<HospitalDoctorDto?> GetDoctorByIdAsync(Guid doctorProfileId, CancellationToken ct = default)
    {
        var doctor = await _hospitalDbContext.DoctorProfiles
            .AsNoTracking()
            .Include(x => x.StaffProfile)
                .ThenInclude(x => x.Department)
            .Include(x => x.Specialty)
            .Include(x => x.DoctorSchedules)
                .ThenInclude(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == doctorProfileId, ct);

        return doctor == null ? null : MapDoctor(doctor);
    }

    private static HospitalDoctorDto MapDoctor(ERMSystem.Infrastructure.HospitalData.Entities.HospitalDoctorProfileEntity doctor)
    {
        return new HospitalDoctorDto
        {
            DoctorProfileId = doctor.Id,
            StaffProfileId = doctor.StaffProfileId,
            SpecialtyId = doctor.SpecialtyId,
            FullName = doctor.StaffProfile.FullName,
            SpecialtyName = doctor.Specialty.Name,
            DepartmentName = doctor.StaffProfile.Department?.Name ?? string.Empty,
            PhotoUrl = BuildDoctorPhotoUrl(doctor.StaffProfile.FullName, doctor.Specialty.Name),
            LicenseNumber = doctor.LicenseNumber,
            Biography = doctor.Biography,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            IsBookable = doctor.IsBookable,
            Schedules = doctor.DoctorSchedules
                .Where(x => x.IsActive)
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.StartTime)
                .Select(x => new HospitalDoctorScheduleDto
                {
                    ScheduleId = x.Id,
                    ClinicId = x.ClinicId,
                    DayOfWeek = x.DayOfWeek,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    SlotMinutes = x.SlotMinutes,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo,
                    ClinicName = x.Clinic.Name,
                    FloorLabel = x.Clinic.FloorLabel,
                    RoomLabel = x.Clinic.RoomLabel
                })
                .ToList()
        };
    }

    private static string BuildDoctorPhotoUrl(string fullName, string specialtyName)
    {
        var initials = BuildInitials(fullName);
        var hue = Math.Abs(fullName.GetHashCode()) % 360;
        var accent = $"hsl({hue}, 70%, 45%)";
        var softAccent = $"hsl({hue}, 75%, 88%)";
        var specialty = WebUtility.HtmlEncode(specialtyName);
        var encodedInitials = WebUtility.HtmlEncode(initials);

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="640" height="760" viewBox="0 0 640 760">
              <defs>
                <linearGradient id="bg" x1="0" x2="1" y1="0" y2="1">
                  <stop offset="0%" stop-color="{softAccent}"/>
                  <stop offset="100%" stop-color="#f8fafc"/>
                </linearGradient>
              </defs>
              <rect width="640" height="760" fill="url(#bg)"/>
              <circle cx="320" cy="250" r="118" fill="#ffffff" opacity="0.95"/>
              <circle cx="320" cy="230" r="70" fill="{accent}" opacity="0.88"/>
              <path d="M176 606c18-112 78-176 144-176s126 64 144 176" fill="{accent}" opacity="0.9"/>
              <path d="M168 610h304c28 0 50 22 50 50v100H118V660c0-28 22-50 50-50z" fill="#0f172a" opacity="0.92"/>
              <text x="320" y="251" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial, sans-serif" font-size="54" font-weight="700" fill="#ffffff">{encodedInitials}</text>
              <text x="320" y="700" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="28" font-weight="700" fill="#ffffff">{specialty}</text>
            </svg>
            """;

        return $"data:image/svg+xml;utf8,{Uri.EscapeDataString(svg)}";
    }

    private static string BuildInitials(string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "BS";
        }

        var last = parts[^1];
        var previous = parts.Length > 1 ? parts[^2] : parts[0];
        return string.Concat(previous[0], last[0]).ToUpperInvariant();
    }
}
