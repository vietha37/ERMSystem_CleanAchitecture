using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Utilities;

namespace ERMSystem.Application.Services;

public class HospitalEncounterService : IHospitalEncounterService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] AllowedStatuses = ["InProgress", "Finalized", "Approved"];

    private readonly IHospitalEncounterRepository _hospitalEncounterRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
    private readonly IBusinessMetricsRecorder _businessMetricsRecorder;
    private readonly IHospitalDocumentStorageService _hospitalDocumentStorageService;
    private readonly IHospitalDoctorWorklistRepository _hospitalDoctorWorklistRepository;

    public HospitalEncounterService(
        IHospitalEncounterRepository hospitalEncounterRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService,
        IBusinessMetricsRecorder businessMetricsRecorder,
        IHospitalDocumentStorageService hospitalDocumentStorageService,
        IHospitalDoctorWorklistRepository hospitalDoctorWorklistRepository)
    {
        _hospitalEncounterRepository = hospitalEncounterRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        _businessMetricsRecorder = businessMetricsRecorder;
        _hospitalDocumentStorageService = hospitalDocumentStorageService;
        _hospitalDoctorWorklistRepository = hospitalDoctorWorklistRepository;
    }

    public Task<PaginatedResult<HospitalEncounterSummaryDto>> GetWorklistAsync(
        HospitalEncounterWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
        => GetScopedWorklistAsync(request, currentRole, currentUsername, ct);

    public async Task<HospitalEncounterDetailDto?> GetByIdAsync(
        Guid encounterId,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var encounter = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (encounter == null)
        {
            return null;
        }

        if (!await CanAccessDoctorScopedEncounterAsync(encounter.DoctorProfileId, currentRole, currentUsername, ct))
        {
            return null;
        }

        return MapDetail(encounter);
    }

    public async Task<HospitalEncounterEligibleAppointmentDto[]> GetEligibleAppointmentsAsync(
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var items = await _hospitalEncounterRepository.GetEligibleAppointmentsAsync(ct);
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        return doctorProfileId.HasValue
            ? items.Where(x => x.DoctorProfileId == doctorProfileId.Value).ToArray()
            : items;
    }

    public async Task<HospitalEncounterDetailDto> CreateAsync(
        CreateHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var normalizedStatus = NormalizeEditableStatus(request.EncounterStatus);
        var appointment = await _hospitalEncounterRepository.GetAppointmentForEncounterAsync(request.AppointmentId, ct);
        if (appointment == null)
        {
            throw new KeyNotFoundException("Khong tim thay lich hen de mo encounter.");
        }

        if (appointment.ExistingEncounterId.HasValue)
        {
            throw new InvalidOperationException("Lich hen nay da co encounter.");
        }

        if (appointment.AppointmentStatus is not ("CheckedIn" or "Completed"))
        {
            throw new InvalidOperationException("Chi duoc mo encounter tu lich hen da check-in hoac da hoan thanh.");
        }

        var nowUtc = DateTime.UtcNow;
        var encounterId = Guid.NewGuid();

        await _hospitalEncounterRepository.AddEncounterAsync(new HospitalEncounterCreateCommand
        {
            EncounterId = encounterId,
            EncounterNumber = GenerateEncounterNumber(nowUtc),
            PatientId = appointment.PatientId,
            AppointmentId = appointment.AppointmentId,
            DoctorProfileId = appointment.DoctorProfileId,
            ClinicId = appointment.ClinicId,
            EncounterType = "Outpatient",
            EncounterStatus = normalizedStatus,
            StartedAtUtc = appointment.AppointmentStartUtc,
            EndedAtUtc = normalizedStatus == "Finalized" ? nowUtc : null,
            Summary = NormalizeText(request.Summary),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        }, ct);

        await _hospitalEncounterRepository.AddVitalSignAsync(new HospitalEncounterVitalSignCreateCommand
        {
            VitalSignId = Guid.NewGuid(),
            EncounterId = encounterId,
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            TemperatureC = request.TemperatureC,
            PulseRate = request.PulseRate,
            RespiratoryRate = request.RespiratoryRate,
            SystolicBp = request.SystolicBp,
            DiastolicBp = request.DiastolicBp,
            OxygenSaturation = request.OxygenSaturation,
            RecordedAtUtc = nowUtc,
            RecordedByUserId = actorUserId
        }, ct);

        await _hospitalEncounterRepository.AddDiagnosisAsync(new HospitalEncounterDiagnosisCreateCommand
        {
            DiagnosisId = Guid.NewGuid(),
            EncounterId = encounterId,
            DiagnosisType = NormalizeDiagnosisType(request.DiagnosisType),
            DiagnosisCode = NormalizeText(request.DiagnosisCode),
            DiagnosisName = NormalizeRequiredText(request.DiagnosisName, "Chan doan"),
            IsPrimary = true,
            NotedAtUtc = nowUtc
        }, ct);

        await _hospitalEncounterRepository.AddClinicalNoteAsync(new HospitalEncounterClinicalNoteCreateCommand
        {
            ClinicalNoteId = Guid.NewGuid(),
            EncounterId = encounterId,
            NoteType = "Consultation",
            Subjective = NormalizeText(request.Subjective),
            Objective = NormalizeText(request.Objective),
            Assessment = NormalizeText(request.Assessment),
            CarePlan = NormalizeText(request.CarePlan),
            AuthoredByUserId = actorUserId,
            AuthoredAtUtc = nowUtc,
            SignedAtUtc = normalizedStatus == "Finalized" ? nowUtc : null
        }, ct);

        if (normalizedStatus == "Finalized")
        {
            await QueueFinalizedEventAsync(encounterId, appointment, request.DiagnosisName, nowUtc, ct);
        }

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_encounter", "created", new Dictionary<string, string?>
        {
            ["status"] = normalizedStatus
        });

        if (normalizedStatus == "Finalized")
        {
            _businessMetricsRecorder.IncrementEvent("hospital_encounter", "finalized");
        }

        var created = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai encounter sau khi tao.");

        return MapDetail(created);
    }

    public async Task<HospitalEncounterDetailDto?> UpdateAsync(
        Guid encounterId,
        UpdateHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var existing = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (existing == null)
        {
            return null;
        }

        if (existing.EncounterStatus == "Approved")
        {
            throw new InvalidOperationException("Ho so da duyet khong duoc cap nhat bang luong sua thong thuong.");
        }

        var normalizedStatus = NormalizeEditableStatus(request.EncounterStatus);
        var nowUtc = DateTime.UtcNow;
        var finalizedNow = existing.EncounterStatus != "Finalized" && normalizedStatus == "Finalized";

        await _hospitalEncounterRepository.UpdateEncounterAsync(new HospitalEncounterUpdateCommand
        {
            EncounterId = encounterId,
            EncounterStatus = normalizedStatus,
            EndedAtUtc = normalizedStatus == "Finalized"
                ? (existing.EndedAtUtc ?? nowUtc)
                : null,
            Summary = NormalizeText(request.Summary),
            UpdatedAtUtc = nowUtc
        }, ct);

        if (existing.VitalSignId.HasValue)
        {
            await _hospitalEncounterRepository.UpdateVitalSignAsync(new HospitalEncounterVitalSignUpdateCommand
            {
                VitalSignId = existing.VitalSignId.Value,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                TemperatureC = request.TemperatureC,
                PulseRate = request.PulseRate,
                RespiratoryRate = request.RespiratoryRate,
                SystolicBp = request.SystolicBp,
                DiastolicBp = request.DiastolicBp,
                OxygenSaturation = request.OxygenSaturation,
                RecordedAtUtc = nowUtc,
                RecordedByUserId = actorUserId
            }, ct);
        }
        else
        {
            await _hospitalEncounterRepository.AddVitalSignAsync(new HospitalEncounterVitalSignCreateCommand
            {
                VitalSignId = Guid.NewGuid(),
                EncounterId = encounterId,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                TemperatureC = request.TemperatureC,
                PulseRate = request.PulseRate,
                RespiratoryRate = request.RespiratoryRate,
                SystolicBp = request.SystolicBp,
                DiastolicBp = request.DiastolicBp,
                OxygenSaturation = request.OxygenSaturation,
                RecordedAtUtc = nowUtc,
                RecordedByUserId = actorUserId
            }, ct);
        }

        if (existing.DiagnosisId.HasValue)
        {
            await _hospitalEncounterRepository.UpdateDiagnosisAsync(new HospitalEncounterDiagnosisUpdateCommand
            {
                DiagnosisId = existing.DiagnosisId.Value,
                DiagnosisType = NormalizeDiagnosisType(request.DiagnosisType),
                DiagnosisCode = NormalizeText(request.DiagnosisCode),
                DiagnosisName = NormalizeRequiredText(request.DiagnosisName, "Chan doan"),
                NotedAtUtc = nowUtc
            }, ct);
        }
        else
        {
            await _hospitalEncounterRepository.AddDiagnosisAsync(new HospitalEncounterDiagnosisCreateCommand
            {
                DiagnosisId = Guid.NewGuid(),
                EncounterId = encounterId,
                DiagnosisType = NormalizeDiagnosisType(request.DiagnosisType),
                DiagnosisCode = NormalizeText(request.DiagnosisCode),
                DiagnosisName = NormalizeRequiredText(request.DiagnosisName, "Chan doan"),
                IsPrimary = true,
                NotedAtUtc = nowUtc
            }, ct);
        }

        DateTime? signedAtUtc = normalizedStatus == "Finalized" ? nowUtc : null;

        if (existing.ClinicalNoteId.HasValue)
        {
            await _hospitalEncounterRepository.UpdateClinicalNoteAsync(new HospitalEncounterClinicalNoteUpdateCommand
            {
                ClinicalNoteId = existing.ClinicalNoteId.Value,
                NoteType = "Consultation",
                Subjective = NormalizeText(request.Subjective),
                Objective = NormalizeText(request.Objective),
                Assessment = NormalizeText(request.Assessment),
                CarePlan = NormalizeText(request.CarePlan),
                AuthoredByUserId = actorUserId,
                AuthoredAtUtc = nowUtc,
                SignedAtUtc = signedAtUtc
            }, ct);
        }
        else
        {
            await _hospitalEncounterRepository.AddClinicalNoteAsync(new HospitalEncounterClinicalNoteCreateCommand
            {
                ClinicalNoteId = Guid.NewGuid(),
                EncounterId = encounterId,
                NoteType = "Consultation",
                Subjective = NormalizeText(request.Subjective),
                Objective = NormalizeText(request.Objective),
                Assessment = NormalizeText(request.Assessment),
                CarePlan = NormalizeText(request.CarePlan),
                AuthoredByUserId = actorUserId,
                AuthoredAtUtc = nowUtc,
                SignedAtUtc = signedAtUtc
            }, ct);
        }

        if (finalizedNow && existing.AppointmentId.HasValue)
        {
            var appointment = await _hospitalEncounterRepository.GetAppointmentForEncounterAsync(existing.AppointmentId.Value, ct)
                ?? throw new InvalidOperationException("Khong tai lai duoc lich hen cua encounter.");
            await QueueFinalizedEventAsync(encounterId, appointment, request.DiagnosisName, nowUtc, ct);
        }

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        if (finalizedNow)
        {
            _businessMetricsRecorder.IncrementEvent("hospital_encounter", "finalized");
        }

        var updated = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        return updated == null ? null : MapDetail(updated);
    }

    public async Task<HospitalEncounterDetailDto?> ApproveAsync(
        Guid encounterId,
        ApproveHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var existing = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (existing == null)
        {
            return null;
        }

        if (existing.EncounterStatus == "InProgress")
        {
            throw new InvalidOperationException("Chi duoc duyet ho so da chot.");
        }

        if (!existing.ClinicalNoteSignedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Ho so can duoc ky xac nhan truoc khi duyet.");
        }

        var nowUtc = DateTime.UtcNow;
        var approvalComment = NormalizeText(request.ApprovalComment) ?? "Clinical record approval completed.";
        await _hospitalEncounterRepository.UpdateEncounterAsync(new HospitalEncounterUpdateCommand
        {
            EncounterId = encounterId,
            EncounterStatus = "Approved",
            EndedAtUtc = existing.EndedAtUtc ?? nowUtc,
            Summary = existing.Summary,
            UpdatedAtUtc = nowUtc
        }, ct);

        if (existing.ApprovalNoteId.HasValue)
        {
            await _hospitalEncounterRepository.UpdateClinicalNoteAsync(new HospitalEncounterClinicalNoteUpdateCommand
            {
                ClinicalNoteId = existing.ApprovalNoteId.Value,
                NoteType = "Approval",
                Subjective = null,
                Objective = null,
                Assessment = "Approved",
                CarePlan = approvalComment,
                AuthoredByUserId = actorUserId,
                AuthoredAtUtc = nowUtc,
                SignedAtUtc = nowUtc
            }, ct);
        }
        else
        {
            await _hospitalEncounterRepository.AddClinicalNoteAsync(new HospitalEncounterClinicalNoteCreateCommand
            {
                ClinicalNoteId = Guid.NewGuid(),
                EncounterId = encounterId,
                NoteType = "Approval",
                Subjective = null,
                Objective = null,
                Assessment = "Approved",
                CarePlan = approvalComment,
                AuthoredByUserId = actorUserId,
                AuthoredAtUtc = nowUtc,
                SignedAtUtc = nowUtc
            }, ct);
        }

        await _hospitalEncounterRepository.AddOutboxMessageAsync(new HospitalEncounterOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Encounter",
            AggregateId = encounterId,
            EventType = "MedicalRecordApproved.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                encounterId,
                approvedAtUtc = nowUtc,
                approvedByUserId = actorUserId,
                approvalComment
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_encounter", "approved");

        var updated = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        return updated == null ? null : MapDetail(updated);
    }

    public async Task<HospitalEncounterDetailDto?> SignAsync(
        Guid encounterId,
        SignHospitalEncounterDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var existing = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (existing == null)
        {
            return null;
        }

        if (existing.EncounterStatus == "InProgress")
        {
            throw new InvalidOperationException("Chi duoc ky xac nhan ho so da chot.");
        }

        if (!existing.ClinicalNoteId.HasValue)
        {
            throw new InvalidOperationException("Ho so chua co ghi chu lam sang de ky.");
        }

        var nowUtc = DateTime.UtcNow;
        await _hospitalEncounterRepository.UpdateClinicalNoteAsync(new HospitalEncounterClinicalNoteUpdateCommand
        {
            ClinicalNoteId = existing.ClinicalNoteId.Value,
            NoteType = "Consultation",
            Subjective = existing.Subjective,
            Objective = existing.Objective,
            Assessment = existing.Assessment,
            CarePlan = existing.CarePlan,
            AuthoredByUserId = actorUserId,
            AuthoredAtUtc = existing.ClinicalNoteAuthoredAtUtc ?? nowUtc,
            SignedAtUtc = nowUtc
        }, ct);

        await _hospitalEncounterRepository.AddClinicalNoteAsync(new HospitalEncounterClinicalNoteCreateCommand
        {
            ClinicalNoteId = Guid.NewGuid(),
            EncounterId = encounterId,
            NoteType = "Signature",
            Subjective = null,
            Objective = null,
            Assessment = "Signed",
            CarePlan = NormalizeText(request.AttestationText) ?? "Clinical note signed and attested.",
            AuthoredByUserId = actorUserId,
            AuthoredAtUtc = nowUtc,
            SignedAtUtc = nowUtc
        }, ct);

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_encounter", "signed");

        var updated = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        return updated == null ? null : MapDetail(updated);
    }

    public async Task<HospitalEncounterDetailDto?> AddAttachmentAsync(
        Guid encounterId,
        AddHospitalEncounterAttachmentDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var encounter = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (encounter == null)
        {
            return null;
        }

        var nowUtc = DateTime.UtcNow;
        await _hospitalEncounterRepository.AddAttachmentAsync(new HospitalEncounterAttachmentCreateCommand
        {
            AttachmentId = Guid.NewGuid(),
            EncounterId = encounterId,
            DocumentType = NormalizeDocumentType(request.DocumentType),
            FileName = NormalizeRequiredText(request.FileName, "Ten tep"),
            ContentType = NormalizeContentType(request.ContentType),
            DocumentUri = NormalizeRequiredText(request.DocumentUri, "Duong dan tai lieu"),
            UploadedAtUtc = nowUtc,
            UploadedByUserId = actorUserId
        }, ct);

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_encounter", "attachment_added", new Dictionary<string, string?>
        {
            ["document_type"] = NormalizeDocumentType(request.DocumentType)
        });

        var updated = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        return updated == null ? null : MapDetail(updated);
    }

    public async Task<HospitalEncounterDetailDto?> UploadAttachmentAsync(
        Guid encounterId,
        string documentType,
        string fileName,
        string? contentType,
        long contentLength,
        Stream content,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var encounter = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        if (encounter == null)
        {
            return null;
        }

        var storedDocument = await _hospitalDocumentStorageService.StoreEncounterAttachmentAsync(
            encounterId,
            fileName,
            contentType,
            contentLength,
            content,
            ct);

        var nowUtc = DateTime.UtcNow;
        await _hospitalEncounterRepository.AddAttachmentAsync(new HospitalEncounterAttachmentCreateCommand
        {
            AttachmentId = Guid.NewGuid(),
            EncounterId = encounterId,
            DocumentType = NormalizeDocumentType(documentType),
            FileName = storedDocument.FileName,
            ContentType = NormalizeContentType(storedDocument.ContentType),
            DocumentUri = storedDocument.StorageUri,
            UploadedAtUtc = nowUtc,
            UploadedByUserId = actorUserId
        }, ct);

        await _hospitalEncounterRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_encounter", "attachment_uploaded", new Dictionary<string, string?>
        {
            ["document_type"] = NormalizeDocumentType(documentType)
        });

        var updated = await _hospitalEncounterRepository.GetEncounterAggregateAsync(encounterId, ct);
        return updated == null ? null : MapDetail(updated);
    }

    public async Task<HospitalStoredAttachmentContentDto?> GetAttachmentContentAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var attachment = await _hospitalEncounterRepository.GetAttachmentAsync(encounterId, attachmentId, ct);
        if (attachment == null)
        {
            return null;
        }

        var storedDocument = await _hospitalDocumentStorageService.OpenReadAsync(attachment.DocumentUri, ct);
        if (storedDocument == null)
        {
            return null;
        }

        return new HospitalStoredAttachmentContentDto
        {
            FileName = attachment.FileName,
            ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? storedDocument.ContentType
                : attachment.ContentType,
            Content = storedDocument.Content,
            ContentLength = storedDocument.ContentLength
        };
    }

    public async Task<HospitalEncounterAttachmentDownloadTicketDto?> CreateAttachmentDownloadTicketAsync(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var attachment = await _hospitalEncounterRepository.GetAttachmentAsync(encounterId, attachmentId, ct);
        if (attachment == null)
        {
            return null;
        }

        var ticket = await _hospitalDocumentStorageService.CreateReadTicketAsync(
            attachment.DocumentUri,
            attachment.FileName,
            attachment.ContentType,
            ct);

        return new HospitalEncounterAttachmentDownloadTicketDto
        {
            StorageProvider = ticket.Provider,
            AccessMode = ticket.AccessMode,
            AccessToken = ticket.AccessToken,
            DownloadUrl = ticket.DownloadUrl ?? string.Empty,
            ExpiresAtUtc = ticket.ExpiresAtUtc,
        };
    }

    public async Task<HospitalStoredAttachmentContentDto?> GetAttachmentContentByTicketAsync(
        string accessToken,
        long? expiresUnixSeconds = null,
        string? signature = null,
        CancellationToken ct = default)
    {
        var storedDocument = await _hospitalDocumentStorageService.OpenReadByTicketAsync(accessToken, expiresUnixSeconds, signature, ct);
        if (storedDocument == null)
        {
            return null;
        }

        return new HospitalStoredAttachmentContentDto
        {
            FileName = storedDocument.FileName,
            ContentType = storedDocument.ContentType,
            Content = storedDocument.Content,
            ContentLength = storedDocument.ContentLength
        };
    }

    private async Task QueueFinalizedEventAsync(
        Guid encounterId,
        HospitalEncounterAppointmentSnapshot appointment,
        string diagnosisName,
        DateTime nowUtc,
        CancellationToken ct)
    {
        await _hospitalEncounterRepository.AddOutboxMessageAsync(new HospitalEncounterOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Encounter",
            AggregateId = encounterId,
            EventType = "MedicalRecordFinalized.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                encounterId,
                appointmentId = appointment.AppointmentId,
                appointment.AppointmentNumber,
                appointment.PatientId,
                appointment.PatientName,
                appointment.MedicalRecordNumber,
                phone = appointment.PatientPhone,
                email = appointment.PatientEmail,
                appointment.DoctorProfileId,
                appointment.DoctorName,
                appointment.SpecialtyName,
                appointment.ClinicName,
                diagnosisName = NormalizeRequiredText(diagnosisName, "Chan doan"),
                finalizedAtUtc = nowUtc
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);
    }

    private Task<Guid?> ResolveHospitalActorUserIdAsync(Guid? actorUserId, string? actorUsername, CancellationToken ct)
        => _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);

    private static string NormalizeStatus(string? status)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "InProgress" : status.Trim();
        if (!AllowedStatuses.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Trang thai encounter khong hop le.");
        }

        return AllowedStatuses.First(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeEditableStatus(string? status)
    {
        var normalized = NormalizeStatus(status);
        if (normalized == "Approved")
        {
            throw new InvalidOperationException("Khong the tao/cap nhat truc tiep ho so o trang thai Approved. Hay dung luong duyet rieng.");
        }

        return normalized;
    }

    private static string NormalizeDiagnosisType(string? diagnosisType)
    {
        return string.IsNullOrWhiteSpace(diagnosisType) ? "Working" : diagnosisType.Trim();
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{fieldName} la truong bat buoc.");
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeDocumentType(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized) ? "EncounterAttachment" : normalized;
    }

    private static string NormalizeContentType(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized) ? "application/octet-stream" : normalized;
    }

    private async Task<PaginatedResult<HospitalEncounterSummaryDto>> GetScopedWorklistAsync(
        HospitalEncounterWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase) && !doctorProfileId.HasValue)
        {
            return new PaginatedResult<HospitalEncounterSummaryDto>(
                Array.Empty<HospitalEncounterSummaryDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        if (doctorProfileId.HasValue)
        {
            request.DoctorProfileId = doctorProfileId.Value;
        }

        return await _hospitalEncounterRepository.GetWorklistAsync(request, ct);
    }

    private async Task<Guid?> ResolveScopedDoctorProfileIdAsync(
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        if (!string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentUsername))
        {
            return null;
        }

        var doctorProfile = await _hospitalDoctorWorklistRepository.ResolveDoctorByUsernameAsync(currentUsername, ct);
        return doctorProfile?.DoctorProfileId;
    }

    private async Task<bool> CanAccessDoctorScopedEncounterAsync(
        Guid encounterDoctorProfileId,
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        var scopedDoctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        return !scopedDoctorProfileId.HasValue || scopedDoctorProfileId.Value == encounterDoctorProfileId;
    }

    private static string GenerateEncounterNumber(DateTime nowUtc)
        => CompactCodeGenerator.Generate("EN", nowUtc);

    private HospitalEncounterDetailDto MapDetail(HospitalEncounterAggregateSnapshot encounter)
    {
        return new HospitalEncounterDetailDto
        {
            EncounterId = encounter.EncounterId,
            EncounterNumber = encounter.EncounterNumber,
            AppointmentId = encounter.AppointmentId,
            AppointmentNumber = encounter.AppointmentNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.PatientName,
            MedicalRecordNumber = encounter.MedicalRecordNumber,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorName,
            SpecialtyName = encounter.SpecialtyName,
            ClinicName = encounter.ClinicName,
            AppointmentStartLocal = encounter.AppointmentStartUtc.HasValue
                ? ConvertUtcToClinicLocal(encounter.AppointmentStartUtc.Value)
                : null,
            EncounterStatus = encounter.EncounterStatus,
            PrimaryDiagnosisName = encounter.DiagnosisName,
            Summary = encounter.Summary,
            StartedAtLocal = ConvertUtcToClinicLocal(encounter.StartedAtUtc),
            EndedAtLocal = encounter.EndedAtUtc.HasValue ? ConvertUtcToClinicLocal(encounter.EndedAtUtc.Value) : null,
            UpdatedAtLocal = ConvertUtcToClinicLocal(encounter.UpdatedAtUtc),
            EncounterType = encounter.EncounterType,
            DiagnosisCode = encounter.DiagnosisCode,
            DiagnosisType = encounter.DiagnosisType,
            ClinicalNoteAuthoredAtLocal = encounter.ClinicalNoteAuthoredAtUtc.HasValue
                ? ConvertUtcToClinicLocal(encounter.ClinicalNoteAuthoredAtUtc.Value)
                : null,
            ClinicalNoteSignedAtLocal = encounter.ClinicalNoteSignedAtUtc.HasValue
                ? ConvertUtcToClinicLocal(encounter.ClinicalNoteSignedAtUtc.Value)
                : null,
            IsClinicalNoteSigned = encounter.ClinicalNoteSignedAtUtc.HasValue,
            ClinicalNoteSignedByUsername = encounter.ClinicalNoteSignedByUsername,
            ApprovalSignedAtLocal = encounter.ApprovalSignedAtUtc.HasValue
                ? ConvertUtcToClinicLocal(encounter.ApprovalSignedAtUtc.Value)
                : null,
            ApprovedByUsername = encounter.ApprovedByUsername,
            ApprovalComment = encounter.ApprovalComment,
            IsApproved = encounter.EncounterStatus == "Approved",
            Subjective = encounter.Subjective,
            Objective = encounter.Objective,
            Assessment = encounter.Assessment,
            CarePlan = encounter.CarePlan,
            HeightCm = encounter.HeightCm,
            WeightKg = encounter.WeightKg,
            TemperatureC = encounter.TemperatureC,
            PulseRate = encounter.PulseRate,
            RespiratoryRate = encounter.RespiratoryRate,
            SystolicBp = encounter.SystolicBp,
            DiastolicBp = encounter.DiastolicBp,
            OxygenSaturation = encounter.OxygenSaturation,
            WorkflowEvents = encounter.WorkflowEvents
                .Select(workflowEvent => new HospitalEncounterWorkflowEventDto
                {
                    EventType = workflowEvent.EventType,
                    Label = GetWorkflowEventLabel(workflowEvent.EventType),
                    Comment = workflowEvent.Comment,
                    PerformedByUsername = workflowEvent.PerformedByUsername,
                    OccurredAtLocal = ConvertUtcToClinicLocal(workflowEvent.OccurredAtUtc)
                })
                .ToList(),
            Attachments = encounter.Attachments
                .Select(attachment => new HospitalEncounterAttachmentDto
                {
                    AttachmentId = attachment.AttachmentId,
                    DocumentType = attachment.DocumentType,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    StorageProvider = ResolveStorageProvider(attachment.DocumentUri),
                    DocumentUri = attachment.DocumentUri,
                    UploadedAtLocal = ConvertUtcToClinicLocal(attachment.UploadedAtUtc),
                    UploadedByUserId = attachment.UploadedByUserId,
                    UploadedByUsername = attachment.UploadedByUsername
                })
                .ToList()
        };
    }

    private string ResolveStorageProvider(string? documentUri)
    {
        var normalized = documentUri?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return _hospitalDocumentStorageService.Provider;
        }

        var schemeSeparatorIndex = normalized.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparatorIndex <= 0)
        {
            return _hospitalDocumentStorageService.Provider;
        }

        return normalized[..schemeSeparatorIndex];
    }

    private static string GetWorkflowEventLabel(string eventType)
    {
        return eventType.Trim().ToLowerInvariant() switch
        {
            "consultation" => "Chốt hồ sơ lâm sàng",
            "signature" => "Ký xác nhận hồ sơ",
            "approval" => "Duyệt hồ sơ",
            _ => eventType
        };
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
}
