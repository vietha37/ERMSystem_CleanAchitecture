using System;
using System.Linq;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services;

public class HospitalDoctorWorklistService : IHospitalDoctorWorklistService
{
    private readonly IHospitalDoctorWorklistRepository _hospitalDoctorWorklistRepository;

    public HospitalDoctorWorklistService(IHospitalDoctorWorklistRepository hospitalDoctorWorklistRepository)
    {
        _hospitalDoctorWorklistRepository = hospitalDoctorWorklistRepository;
    }

    public async Task<HospitalDoctorWorklistResponseDto> GetWorklistAsync(
        HospitalDoctorWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var workDate = request.WorkDate ?? DateOnly.FromDateTime(GetClinicNow().Date);
        Guid? doctorProfileId = request.DoctorProfileId;
        HospitalDoctorProfileSnapshot? doctorProfile = null;

        if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(currentUsername))
            {
                return new HospitalDoctorWorklistResponseDto
                {
                    WorkDate = workDate,
                    IsDoctorResolved = false,
                    ResolutionMessage = "Khong xac dinh duoc tai khoan bac si hien tai."
                };
            }

            doctorProfile = await _hospitalDoctorWorklistRepository.ResolveDoctorByUsernameAsync(currentUsername, ct);
            if (doctorProfile == null)
            {
                return new HospitalDoctorWorklistResponseDto
                {
                    WorkDate = workDate,
                    IsDoctorResolved = false,
                    ResolutionMessage = "Tai khoan nay chua duoc gan voi doctor profile trong hospital database."
                };
            }

            doctorProfileId = doctorProfile.DoctorProfileId;
        }
        else if (doctorProfileId.HasValue)
        {
            doctorProfile = await _hospitalDoctorWorklistRepository.GetDoctorProfileAsync(doctorProfileId.Value, ct);
        }

        var snapshots = await _hospitalDoctorWorklistRepository.GetWorklistAsync(workDate, doctorProfileId, ct);
        var items = snapshots.Select(MapItem).ToList();

        return new HospitalDoctorWorklistResponseDto
        {
            WorkDate = workDate,
            DoctorProfileId = doctorProfile?.DoctorProfileId ?? doctorProfileId,
            DoctorName = doctorProfile?.DoctorName,
            SpecialtyName = doctorProfile?.SpecialtyName,
            IsDoctorResolved = !string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase) || doctorProfile != null,
            ResolutionMessage = doctorProfile == null && string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase)
                ? "Tai khoan nay chua duoc gan voi doctor profile trong hospital database."
                : null,
            TotalAppointments = items.Count,
            CheckedInAppointments = items.Count(x => x.AppointmentStatus == "CheckedIn"),
            InProgressEncounters = items.Count(x => x.EncounterStatus == "InProgress"),
            FinalizedEncounters = items.Count(x => x.EncounterStatus is "Finalized" or "Approved"),
            IssuedPrescriptions = items.Count(x => x.PrescriptionId.HasValue),
            Items = items
        };
    }

    private static HospitalDoctorWorklistItemDto MapItem(HospitalDoctorWorklistSnapshot snapshot)
    {
        return new HospitalDoctorWorklistItemDto
        {
            AppointmentId = snapshot.AppointmentId,
            AppointmentNumber = snapshot.AppointmentNumber,
            AppointmentStatus = snapshot.AppointmentStatus,
            AppointmentStartLocal = ConvertUtcToClinicLocal(snapshot.AppointmentStartUtc),
            PatientId = snapshot.PatientId,
            PatientName = snapshot.PatientName,
            MedicalRecordNumber = snapshot.MedicalRecordNumber,
            DoctorProfileId = snapshot.DoctorProfileId,
            DoctorName = snapshot.DoctorName,
            SpecialtyName = snapshot.SpecialtyName,
            ClinicName = snapshot.ClinicName,
            EncounterId = snapshot.EncounterId,
            EncounterNumber = snapshot.EncounterNumber,
            EncounterStatus = snapshot.EncounterStatus,
            PrimaryDiagnosisName = snapshot.PrimaryDiagnosisName,
            PrescriptionId = snapshot.PrescriptionId,
            PrescriptionNumber = snapshot.PrescriptionNumber,
            WorkflowStage = ResolveWorkflowStage(snapshot)
        };
    }

    private static string ResolveWorkflowStage(HospitalDoctorWorklistSnapshot snapshot)
    {
        if (snapshot.PrescriptionId.HasValue)
        {
            return "Da ke don";
        }

        if (snapshot.EncounterStatus == "Approved")
        {
            return "Da duyet ho so";
        }

        if (snapshot.EncounterStatus == "Finalized")
        {
            return "Cho ke don";
        }

        if (snapshot.EncounterStatus == "InProgress")
        {
            return "Dang kham";
        }

        if (snapshot.AppointmentStatus == "CheckedIn")
        {
            return "Cho mo ho so";
        }

        if (snapshot.AppointmentStatus == "Completed")
        {
            return "Da hoan thanh";
        }

        return "Cho tiep don";
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

    private static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), ResolveClinicTimeZone());

    private static DateTime GetClinicNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResolveClinicTimeZone());
}
