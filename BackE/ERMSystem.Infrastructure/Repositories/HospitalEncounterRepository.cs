using System;
using System.Linq;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalEncounterRepository : IHospitalEncounterRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalEncounterRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<PaginatedResult<HospitalEncounterSummaryDto>> GetWorklistAsync(
        HospitalEncounterWorklistRequestDto request,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Appointment)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.Diagnoses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EncounterStatus))
        {
            var normalizedStatus = request.EncounterStatus.Trim();
            query = query.Where(x => x.EncounterStatus == normalizedStatus);
        }

        if (request.AppointmentDate.HasValue)
        {
            var (fromUtc, toUtc) = BuildUtcRange(request.AppointmentDate.Value);
            query = query.Where(x =>
                (x.Appointment != null && x.Appointment.AppointmentStartUtc >= fromUtc && x.Appointment.AppointmentStartUtc < toUtc) ||
                (x.Appointment == null && x.StartedAtUtc >= fromUtc && x.StartedAtUtc < toUtc));
        }

        if (request.DoctorProfileId.HasValue)
        {
            query = query.Where(x => x.DoctorProfileId == request.DoctorProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TextSearch))
        {
            var keyword = request.TextSearch.Trim();
            var pattern = $"%{keyword}%";
            query = query.Where(x =>
                EF.Functions.Like(x.EncounterNumber, pattern) ||
                (x.Appointment != null && EF.Functions.Like(x.Appointment.AppointmentNumber, pattern)) ||
                EF.Functions.Like(x.Patient.FullName, pattern) ||
                EF.Functions.Like(x.Patient.MedicalRecordNumber, pattern) ||
                EF.Functions.Like(x.DoctorProfile.StaffProfile.FullName, pattern) ||
                (x.Summary != null && EF.Functions.Like(x.Summary, pattern)) ||
                x.Diagnoses.Any(d => EF.Functions.Like(d.DiagnosisName, pattern)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var mapped = items.Select(x =>
        {
            var primaryDiagnosis = x.Diagnoses
                .OrderByDescending(d => d.IsPrimary)
                .ThenByDescending(d => d.NotedAtUtc)
                .FirstOrDefault();

            return new HospitalEncounterSummaryDto
            {
                EncounterId = x.Id,
                EncounterNumber = x.EncounterNumber,
                AppointmentId = x.AppointmentId,
                AppointmentNumber = x.Appointment?.AppointmentNumber,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                MedicalRecordNumber = x.Patient.MedicalRecordNumber,
                DoctorProfileId = x.DoctorProfileId,
                DoctorName = x.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = x.DoctorProfile.Specialty.Name,
                ClinicName = x.Clinic.Name,
                AppointmentStartLocal = x.Appointment?.AppointmentStartUtc is DateTime startUtc
                    ? ConvertUtcToClinicLocal(startUtc)
                    : null,
                EncounterStatus = x.EncounterStatus,
                PrimaryDiagnosisName = primaryDiagnosis?.DiagnosisName,
                Summary = x.Summary,
                StartedAtLocal = ConvertUtcToClinicLocal(x.StartedAtUtc),
                EndedAtLocal = x.EndedAtUtc.HasValue ? ConvertUtcToClinicLocal(x.EndedAtUtc.Value) : null,
                UpdatedAtLocal = ConvertUtcToClinicLocal(x.UpdatedAtUtc)
            };
        }).ToArray();

        return new PaginatedResult<HospitalEncounterSummaryDto>(mapped, totalCount, pageNumber, pageSize);
    }

    public async Task<HospitalEncounterAggregateSnapshot?> GetEncounterAggregateAsync(
        Guid encounterId,
        CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.Encounters
            .Include(x => x.Patient)
            .Include(x => x.Appointment)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.VitalSigns)
            .Include(x => x.Diagnoses)
            .Include(x => x.ClinicalNotes)
                .ThenInclude(x => x.AuthoredByUser)
            .Include(x => x.Attachments)
                .ThenInclude(x => x.UploadedByUser)
            .FirstOrDefaultAsync(x => x.Id == encounterId, ct);

        if (entity == null)
        {
            return null;
        }

        var latestVital = entity.VitalSigns.OrderByDescending(x => x.RecordedAtUtc).FirstOrDefault();
        var primaryDiagnosis = entity.Diagnoses
            .OrderByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.NotedAtUtc)
            .FirstOrDefault();
        var latestNote = entity.ClinicalNotes
            .Where(x => !string.Equals(x.NoteType, "Approval", StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(x.NoteType, "Signature", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.AuthoredAtUtc)
            .FirstOrDefault();
        var latestSignatureNote = entity.ClinicalNotes
            .Where(x => string.Equals(x.NoteType, "Signature", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.AuthoredAtUtc)
            .FirstOrDefault();
        var latestApprovalNote = entity.ClinicalNotes
            .Where(x => string.Equals(x.NoteType, "Approval", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.AuthoredAtUtc)
            .FirstOrDefault();

        return new HospitalEncounterAggregateSnapshot
        {
            EncounterId = entity.Id,
            EncounterNumber = entity.EncounterNumber,
            EncounterType = entity.EncounterType,
            EncounterStatus = entity.EncounterStatus,
            PatientId = entity.PatientId,
            PatientName = entity.Patient.FullName,
            MedicalRecordNumber = entity.Patient.MedicalRecordNumber,
            DoctorProfileId = entity.DoctorProfileId,
            DoctorName = entity.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = entity.DoctorProfile.Specialty.Name,
            ClinicId = entity.ClinicId,
            ClinicName = entity.Clinic.Name,
            AppointmentId = entity.AppointmentId,
            AppointmentNumber = entity.Appointment?.AppointmentNumber,
            AppointmentStartUtc = entity.Appointment?.AppointmentStartUtc,
            StartedAtUtc = entity.StartedAtUtc,
            EndedAtUtc = entity.EndedAtUtc,
            Summary = entity.Summary,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            VitalSignId = latestVital?.Id,
            HeightCm = latestVital?.HeightCm,
            WeightKg = latestVital?.WeightKg,
            TemperatureC = latestVital?.TemperatureC,
            PulseRate = latestVital?.PulseRate,
            RespiratoryRate = latestVital?.RespiratoryRate,
            SystolicBp = latestVital?.SystolicBp,
            DiastolicBp = latestVital?.DiastolicBp,
            OxygenSaturation = latestVital?.OxygenSaturation,
            DiagnosisId = primaryDiagnosis?.Id,
            DiagnosisType = primaryDiagnosis?.DiagnosisType,
            DiagnosisCode = primaryDiagnosis?.DiagnosisCode,
            DiagnosisName = primaryDiagnosis?.DiagnosisName,
            ClinicalNoteId = latestNote?.Id,
            ClinicalNoteAuthoredAtUtc = latestNote?.AuthoredAtUtc,
            ClinicalNoteSignedAtUtc = latestNote?.SignedAtUtc,
            ClinicalNoteSignedByUsername = latestSignatureNote?.AuthoredByUser?.Username
                ?? latestNote?.AuthoredByUser?.Username,
            ApprovalNoteId = latestApprovalNote?.Id,
            ApprovalSignedAtUtc = latestApprovalNote?.SignedAtUtc,
            ApprovedByUsername = latestApprovalNote?.AuthoredByUser?.Username,
            ApprovalComment = latestApprovalNote?.CarePlan,
            Subjective = latestNote?.Subjective,
            Objective = latestNote?.Objective,
            Assessment = latestNote?.Assessment,
            CarePlan = latestNote?.CarePlan,
            WorkflowEvents = entity.ClinicalNotes
                .Where(x =>
                    (x.SignedAtUtc.HasValue && string.Equals(x.NoteType, "Consultation", StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(x.NoteType, "Signature", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.NoteType, "Approval", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.SignedAtUtc ?? x.AuthoredAtUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new HospitalEncounterWorkflowEventSnapshot
                {
                    EventType = x.NoteType,
                    Comment = x.CarePlan,
                    PerformedByUsername = x.AuthoredByUser != null ? x.AuthoredByUser.Username : null,
                    OccurredAtUtc = x.SignedAtUtc ?? x.AuthoredAtUtc
                })
                .ToArray(),
            Attachments = entity.Attachments
                .OrderByDescending(x => x.UploadedAtUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new HospitalEncounterAttachmentSnapshot
                {
                    AttachmentId = x.Id,
                    DocumentType = x.DocumentType,
                    FileName = x.FileName,
                    ContentType = x.MimeType ?? "application/octet-stream",
                    DocumentUri = x.StorageUri,
                    UploadedAtUtc = x.UploadedAtUtc,
                    UploadedByUserId = x.UploadedByUserId,
                    UploadedByUsername = x.UploadedByUser != null ? x.UploadedByUser.Username : null
                })
                .ToArray()
        };
    }

    public async Task<HospitalEncounterAttachmentSnapshot?> GetAttachmentAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        return await _hospitalDbContext.EncounterAttachments
            .AsNoTracking()
            .Where(x => x.EncounterId == encounterId && x.Id == attachmentId)
            .Select(x => new HospitalEncounterAttachmentSnapshot
            {
                AttachmentId = x.Id,
                DocumentType = x.DocumentType,
                FileName = x.FileName,
                ContentType = x.MimeType ?? "application/octet-stream",
                DocumentUri = x.StorageUri,
                UploadedAtUtc = x.UploadedAtUtc,
                UploadedByUserId = x.UploadedByUserId,
                UploadedByUsername = null
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<string[]> GetReferencedAttachmentUrisAsync(CancellationToken ct = default)
    {
        return _hospitalDbContext.EncounterAttachments
            .AsNoTracking()
            .Where(x => x.StorageUri != null && x.StorageUri != "")
            .Select(x => x.StorageUri)
            .Distinct()
            .ToArrayAsync(ct);
    }

    public async Task<HospitalEncounterAppointmentSnapshot?> GetAppointmentForEncounterAsync(
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var appointment = await _hospitalDbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == appointmentId, ct);

        if (appointment == null)
        {
            return null;
        }

        var existingEncounter = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => new { x.Id, x.EncounterNumber })
            .FirstOrDefaultAsync(ct);

        return new HospitalEncounterAppointmentSnapshot
        {
            AppointmentId = appointment.Id,
            AppointmentNumber = appointment.AppointmentNumber,
            AppointmentStatus = appointment.Status,
            AppointmentStartUtc = appointment.AppointmentStartUtc,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient.FullName,
            MedicalRecordNumber = appointment.Patient.MedicalRecordNumber,
            PatientPhone = appointment.Patient.Phone,
            PatientEmail = appointment.Patient.Email,
            DoctorProfileId = appointment.DoctorProfileId,
            DoctorName = appointment.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = appointment.DoctorProfile.Specialty.Name,
            ClinicId = appointment.ClinicId,
            ClinicName = appointment.Clinic.Name,
            ExistingEncounterId = existingEncounter?.Id,
            ExistingEncounterNumber = existingEncounter?.EncounterNumber
        };
    }

    public async Task<HospitalEncounterEligibleAppointmentDto[]> GetEligibleAppointmentsAsync(CancellationToken ct = default)
    {
        var appointments = await _hospitalDbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Where(x => x.Status == "CheckedIn" || x.Status == "Completed")
            .OrderByDescending(x => x.AppointmentStartUtc)
            .Take(100)
            .ToListAsync(ct);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var encounters = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Where(x => x.AppointmentId != null && appointmentIds.Contains(x.AppointmentId.Value))
            .Select(x => new { AppointmentId = x.AppointmentId!.Value, x.Id, x.EncounterNumber })
            .ToListAsync(ct);

        var encounterLookup = encounters.ToDictionary(x => x.AppointmentId, x => x);

        return appointments.Select(x =>
        {
            encounterLookup.TryGetValue(x.Id, out var existingEncounter);
            return new HospitalEncounterEligibleAppointmentDto
            {
                AppointmentId = x.Id,
                AppointmentNumber = x.AppointmentNumber,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                MedicalRecordNumber = x.Patient.MedicalRecordNumber,
                DoctorProfileId = x.DoctorProfileId,
                DoctorName = x.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = x.DoctorProfile.Specialty.Name,
                ClinicName = x.Clinic.Name,
                AppointmentStartLocal = ConvertUtcToClinicLocal(x.AppointmentStartUtc),
                AppointmentStatus = x.Status,
                ExistingEncounterId = existingEncounter?.Id,
                ExistingEncounterNumber = existingEncounter?.EncounterNumber
            };
        }).ToArray();
    }

    public Task<bool> HospitalUserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return _hospitalDbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId, ct);
    }

    public Task AddEncounterAsync(HospitalEncounterCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Encounters.Add(new HospitalEncounterEntity
        {
            Id = command.EncounterId,
            EncounterNumber = command.EncounterNumber,
            PatientId = command.PatientId,
            AppointmentId = command.AppointmentId,
            DoctorProfileId = command.DoctorProfileId,
            ClinicId = command.ClinicId,
            EncounterType = command.EncounterType,
            EncounterStatus = command.EncounterStatus,
            StartedAtUtc = command.StartedAtUtc,
            EndedAtUtc = command.EndedAtUtc,
            Summary = command.Summary,
            CreatedAtUtc = command.CreatedAtUtc,
            UpdatedAtUtc = command.UpdatedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddVitalSignAsync(HospitalEncounterVitalSignCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.VitalSigns.Add(new HospitalVitalSignEntity
        {
            Id = command.VitalSignId,
            EncounterId = command.EncounterId,
            HeightCm = command.HeightCm,
            WeightKg = command.WeightKg,
            TemperatureC = command.TemperatureC,
            PulseRate = command.PulseRate,
            RespiratoryRate = command.RespiratoryRate,
            SystolicBp = command.SystolicBp,
            DiastolicBp = command.DiastolicBp,
            OxygenSaturation = command.OxygenSaturation,
            RecordedAtUtc = command.RecordedAtUtc,
            RecordedByUserId = command.RecordedByUserId
        });

        return Task.CompletedTask;
    }

    public Task AddDiagnosisAsync(HospitalEncounterDiagnosisCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Diagnoses.Add(new HospitalDiagnosisEntity
        {
            Id = command.DiagnosisId,
            EncounterId = command.EncounterId,
            DiagnosisType = command.DiagnosisType,
            DiagnosisCode = command.DiagnosisCode,
            DiagnosisName = command.DiagnosisName,
            IsPrimary = command.IsPrimary,
            NotedAtUtc = command.NotedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddClinicalNoteAsync(HospitalEncounterClinicalNoteCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.ClinicalNotes.Add(new HospitalClinicalNoteEntity
        {
            Id = command.ClinicalNoteId,
            EncounterId = command.EncounterId,
            NoteType = command.NoteType,
            Subjective = command.Subjective,
            Objective = command.Objective,
            Assessment = command.Assessment,
            CarePlan = command.CarePlan,
            AuthoredByUserId = command.AuthoredByUserId,
            AuthoredAtUtc = command.AuthoredAtUtc,
            SignedAtUtc = command.SignedAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task UpdateEncounterAsync(HospitalEncounterUpdateCommand command, CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.Encounters.FirstOrDefaultAsync(x => x.Id == command.EncounterId, ct);
        if (entity == null)
        {
            throw new InvalidOperationException("Khong tim thay encounter.");
        }

        entity.EncounterStatus = command.EncounterStatus;
        entity.EndedAtUtc = command.EndedAtUtc;
        entity.Summary = command.Summary;
        entity.UpdatedAtUtc = command.UpdatedAtUtc;
    }

    public async Task UpdateVitalSignAsync(HospitalEncounterVitalSignUpdateCommand command, CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.VitalSigns.FirstOrDefaultAsync(x => x.Id == command.VitalSignId, ct);
        if (entity == null)
        {
            throw new InvalidOperationException("Khong tim thay ban ghi dau hieu sinh ton.");
        }

        entity.HeightCm = command.HeightCm;
        entity.WeightKg = command.WeightKg;
        entity.TemperatureC = command.TemperatureC;
        entity.PulseRate = command.PulseRate;
        entity.RespiratoryRate = command.RespiratoryRate;
        entity.SystolicBp = command.SystolicBp;
        entity.DiastolicBp = command.DiastolicBp;
        entity.OxygenSaturation = command.OxygenSaturation;
        entity.RecordedAtUtc = command.RecordedAtUtc;
        entity.RecordedByUserId = command.RecordedByUserId;
    }

    public async Task UpdateDiagnosisAsync(HospitalEncounterDiagnosisUpdateCommand command, CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.Diagnoses.FirstOrDefaultAsync(x => x.Id == command.DiagnosisId, ct);
        if (entity == null)
        {
            throw new InvalidOperationException("Khong tim thay chan doan.");
        }

        entity.DiagnosisType = command.DiagnosisType;
        entity.DiagnosisCode = command.DiagnosisCode;
        entity.DiagnosisName = command.DiagnosisName;
        entity.NotedAtUtc = command.NotedAtUtc;
    }

    public async Task UpdateClinicalNoteAsync(HospitalEncounterClinicalNoteUpdateCommand command, CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.ClinicalNotes.FirstOrDefaultAsync(x => x.Id == command.ClinicalNoteId, ct);
        if (entity == null)
        {
            throw new InvalidOperationException("Khong tim thay ghi chu lam sang.");
        }

        entity.Subjective = command.Subjective;
        entity.Objective = command.Objective;
        entity.Assessment = command.Assessment;
        entity.CarePlan = command.CarePlan;
        entity.NoteType = string.IsNullOrWhiteSpace(command.NoteType) ? entity.NoteType : command.NoteType.Trim();
        entity.AuthoredByUserId = command.AuthoredByUserId;
        entity.AuthoredAtUtc = command.AuthoredAtUtc;
        entity.SignedAtUtc = command.SignedAtUtc;
    }

    public Task AddAttachmentAsync(HospitalEncounterAttachmentCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.EncounterAttachments.Add(new HospitalEncounterAttachmentEntity
        {
            Id = command.AttachmentId,
            EncounterId = command.EncounterId,
            DocumentType = command.DocumentType,
            FileName = command.FileName,
            MimeType = command.ContentType,
            StorageUri = command.DocumentUri,
            UploadedAtUtc = command.UploadedAtUtc,
            UploadedByUserId = command.UploadedByUserId
        });

        return Task.CompletedTask;
    }

    public Task AddOutboxMessageAsync(HospitalEncounterOutboxCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.OutboxMessages.Add(new HospitalOutboxMessageEntity
        {
            Id = command.OutboxMessageId,
            AggregateType = command.AggregateType,
            AggregateId = command.AggregateId,
            EventType = command.EventType,
            PayloadJson = command.PayloadJson,
            Status = command.Status,
            AvailableAtUtc = command.AvailableAtUtc
        });

        return Task.CompletedTask;
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
