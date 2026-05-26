using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services;

public class HospitalClinicalOrderService : IHospitalClinicalOrderService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHospitalClinicalOrderRepository _hospitalClinicalOrderRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;

    public HospitalClinicalOrderService(
        IHospitalClinicalOrderRepository hospitalClinicalOrderRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService)
    {
        _hospitalClinicalOrderRepository = hospitalClinicalOrderRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
    }

    public Task<PaginatedResult<HospitalClinicalOrderSummaryDto>> GetWorklistAsync(
        HospitalClinicalOrderWorklistRequestDto request,
        CancellationToken ct = default)
        => _hospitalClinicalOrderRepository.GetWorklistAsync(request, ct);

    public async Task<HospitalClinicalOrderDetailDto?> GetByIdAsync(Guid clinicalOrderId, CancellationToken ct = default)
    {
        var detail = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct);
        return detail == null ? null : MapDetail(detail);
    }

    public Task<HospitalClinicalOrderEligibleEncounterDto[]> GetEligibleEncountersAsync(CancellationToken ct = default)
        => _hospitalClinicalOrderRepository.GetEligibleEncountersAsync(ct);

    public Task<HospitalClinicalOrderCatalogItemDto[]> GetCatalogAsync(CancellationToken ct = default)
        => _hospitalClinicalOrderRepository.GetCatalogAsync(ct);

    public async Task<HospitalClinicalOrderDetailDto> CreateAsync(
        CreateHospitalClinicalOrderDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var normalizedCategory = NormalizeCategory(request.Category);
        var actorHospitalUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var encounter = await _hospitalClinicalOrderRepository.GetEncounterForOrderingAsync(request.EncounterId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay encounter de tao chi dinh.");

        if (encounter.EncounterStatus is not ("InProgress" or "Finalized" or "Approved"))
        {
            throw new InvalidOperationException("Chi duoc tao chi dinh tu encounter dang kham, da chot ho so hoac da duyet.");
        }

        var catalogService = await _hospitalClinicalOrderRepository.GetCatalogServiceAsync(normalizedCategory, request.ServiceId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay dich vu can lam sang.");

        var nowUtc = DateTime.UtcNow;
        var orderHeaderId = Guid.NewGuid();
        var orderNumber = GenerateOrderNumber(normalizedCategory, nowUtc);

        await _hospitalClinicalOrderRepository.AddOrderHeaderAsync(new HospitalClinicalOrderHeaderCreateCommand
        {
            OrderHeaderId = orderHeaderId,
            EncounterId = encounter.EncounterId,
            OrderNumber = orderNumber,
            OrderCategory = normalizedCategory,
            OrderStatus = "Requested",
            OrderedByUserId = actorHospitalUserId,
            OrderedAtUtc = nowUtc
        }, ct);

        Guid clinicalOrderId;
        if (normalizedCategory == "Lab")
        {
            clinicalOrderId = Guid.NewGuid();
            await _hospitalClinicalOrderRepository.AddLabOrderAsync(new HospitalLabOrderCreateCommand
            {
                LabOrderId = clinicalOrderId,
                OrderHeaderId = orderHeaderId,
                LabServiceId = request.ServiceId,
                OrderStatus = "Requested",
                PriorityCode = NormalizeText(request.PriorityCode),
                RequestedAtUtc = nowUtc
            }, ct);
        }
        else
        {
            clinicalOrderId = Guid.NewGuid();
            await _hospitalClinicalOrderRepository.AddImagingOrderAsync(new HospitalImagingOrderCreateCommand
            {
                ImagingOrderId = clinicalOrderId,
                OrderHeaderId = orderHeaderId,
                ImagingServiceId = request.ServiceId,
                OrderStatus = "Requested",
                RequestedAtUtc = nowUtc
            }, ct);
        }

        await _hospitalClinicalOrderRepository.AddOutboxMessageAsync(new HospitalClinicalOrderOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = normalizedCategory + "Order",
            AggregateId = clinicalOrderId,
            EventType = normalizedCategory == "Lab" ? "LabOrderRequested.v1" : "ImagingOrderRequested.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                clinicalOrderId,
                orderHeaderId,
                orderNumber,
                category = normalizedCategory,
                encounter.EncounterId,
                encounter.EncounterNumber,
                encounter.PatientId,
                encounter.PatientName,
                encounter.MedicalRecordNumber,
                encounter.DoctorName,
                serviceId = catalogService.ServiceId,
                serviceCode = catalogService.ServiceCode,
                serviceName = catalogService.ServiceName,
                priorityCode = NormalizeText(request.PriorityCode),
                requestedAtUtc = nowUtc
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalClinicalOrderRepository.SaveChangesAsync(ct);

        var created = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai chi dinh vua tao.");

        return MapDetail(created);
    }

    public async Task<HospitalClinicalOrderDetailDto?> RecordLabResultAsync(
        Guid clinicalOrderId,
        RecordHospitalLabResultDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var actorHospitalUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var detail = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct);
        if (detail == null)
        {
            return null;
        }

        if (detail.Category != "Lab")
        {
            throw new InvalidOperationException("Chi dinh nay khong phai xet nghiem.");
        }

        if (detail.Status == "Completed")
        {
            throw new InvalidOperationException("Chi dinh xet nghiem nay da co ket qua.");
        }

        if (request.ResultItems == null || request.ResultItems.Count == 0)
        {
            throw new InvalidOperationException("Can co it nhat mot dong ket qua xet nghiem.");
        }

        var nowUtc = DateTime.UtcNow;
        if (!detail.SpecimenId.HasValue)
        {
            await _hospitalClinicalOrderRepository.AddSpecimenAsync(new HospitalSpecimenCreateCommand
            {
                SpecimenId = Guid.NewGuid(),
                LabOrderId = clinicalOrderId,
                SpecimenCode = NormalizeText(request.SpecimenCode) ?? GenerateSpecimenCode(nowUtc),
                CollectedAtUtc = nowUtc,
                ReceivedAtUtc = nowUtc,
                Status = "Received"
            }, ct);
        }

        var items = request.ResultItems.Select(item => new HospitalLabResultItemCreateCommand
        {
            ResultItemId = Guid.NewGuid(),
            LabOrderId = clinicalOrderId,
            AnalyteCode = NormalizeText(item.AnalyteCode),
            AnalyteName = NormalizeRequiredText(item.AnalyteName, "Ten chi so"),
            ResultValue = NormalizeText(item.ResultValue),
            Unit = NormalizeText(item.Unit),
            ReferenceRange = NormalizeText(item.ReferenceRange),
            AbnormalFlag = NormalizeText(item.AbnormalFlag),
            VerifiedAtUtc = nowUtc
        }).ToArray();

        await _hospitalClinicalOrderRepository.ReplaceLabResultItemsAsync(clinicalOrderId, items, ct);
        await _hospitalClinicalOrderRepository.UpdateLabOrderCompletionAsync(clinicalOrderId, "Completed", nowUtc, ct);
        await _hospitalClinicalOrderRepository.UpdateOrderHeaderStatusAsync(detail.OrderHeaderId, "Completed", ct);
        await _hospitalClinicalOrderRepository.AddOutboxMessageAsync(new HospitalClinicalOrderOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "LabOrder",
            AggregateId = clinicalOrderId,
            EventType = "LabResultVerified.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                clinicalOrderId,
                detail.OrderHeaderId,
                detail.OrderNumber,
                detail.EncounterId,
                detail.EncounterNumber,
                detail.PatientId,
                detail.PatientName,
                detail.MedicalRecordNumber,
                verifiedAtUtc = nowUtc,
                verifiedByUserId = actorHospitalUserId,
                resultItemCount = items.Length
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalClinicalOrderRepository.SaveChangesAsync(ct);
        var updated = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai ket qua xet nghiem.");

        return MapDetail(updated);
    }

    public async Task<HospitalClinicalOrderDetailDto?> RecordImagingReportAsync(
        Guid clinicalOrderId,
        RecordHospitalImagingReportDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var actorHospitalUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var detail = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct);
        if (detail == null)
        {
            return null;
        }

        if (detail.Category != "Imaging")
        {
            throw new InvalidOperationException("Chi dinh nay khong phai chan doan hinh anh.");
        }

        if (detail.Status == "Completed")
        {
            throw new InvalidOperationException("Chi dinh chan doan hinh anh nay da co bao cao.");
        }

        var nowUtc = DateTime.UtcNow;
        await _hospitalClinicalOrderRepository.AddImagingReportAsync(new HospitalImagingReportCreateCommand
        {
            ImagingReportId = Guid.NewGuid(),
            ImagingOrderId = clinicalOrderId,
            Findings = NormalizeText(request.Findings),
            Impression = NormalizeText(request.Impression),
            ReportUri = NormalizeText(request.ReportUri),
            SignedByUserId = actorHospitalUserId,
            SignedAtUtc = nowUtc
        }, ct);
        await _hospitalClinicalOrderRepository.UpdateImagingOrderCompletionAsync(clinicalOrderId, "Completed", nowUtc, ct);
        await _hospitalClinicalOrderRepository.UpdateOrderHeaderStatusAsync(detail.OrderHeaderId, "Completed", ct);
        await _hospitalClinicalOrderRepository.AddOutboxMessageAsync(new HospitalClinicalOrderOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "ImagingOrder",
            AggregateId = clinicalOrderId,
            EventType = "ImagingReportSigned.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                clinicalOrderId,
                detail.OrderHeaderId,
                detail.OrderNumber,
                detail.EncounterId,
                detail.EncounterNumber,
                detail.PatientId,
                detail.PatientName,
                detail.MedicalRecordNumber,
                signedAtUtc = nowUtc,
                signedByUserId = actorHospitalUserId,
                impression = NormalizeText(request.Impression)
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalClinicalOrderRepository.SaveChangesAsync(ct);
        var updated = await _hospitalClinicalOrderRepository.GetByIdAsync(clinicalOrderId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai bao cao chan doan hinh anh.");

        return MapDetail(updated);
    }

    private Task<Guid?> ResolveHospitalActorUserIdAsync(Guid? actorUserId, string? actorUsername, CancellationToken ct)
        => _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);

    private static HospitalClinicalOrderDetailDto MapDetail(HospitalClinicalOrderDetailSnapshot detail)
    {
        return new HospitalClinicalOrderDetailDto
        {
            ClinicalOrderId = detail.ClinicalOrderId,
            OrderHeaderId = detail.OrderHeaderId,
            OrderNumber = detail.OrderNumber,
            Category = detail.Category,
            Status = detail.Status,
            EncounterId = detail.EncounterId,
            EncounterNumber = detail.EncounterNumber,
            PatientId = detail.PatientId,
            PatientName = detail.PatientName,
            MedicalRecordNumber = detail.MedicalRecordNumber,
            DoctorName = detail.DoctorName,
            SpecialtyName = detail.SpecialtyName,
            ClinicName = detail.ClinicName,
            ServiceCode = detail.ServiceCode,
            ServiceName = detail.ServiceName,
            PriorityCode = detail.PriorityCode,
            RequestedAtLocal = ConvertUtcToClinicLocal(detail.RequestedAtUtc),
            CompletedAtLocal = detail.CompletedAtUtc.HasValue ? ConvertUtcToClinicLocal(detail.CompletedAtUtc.Value) : null,
            OrderedByUsername = detail.OrderedByUsername,
            ResultItemCount = detail.ResultItems.Length,
            SummaryText = detail.Category == "Imaging" ? detail.Impression : detail.ResultItems.FirstOrDefault()?.ResultValue,
            SpecimenId = detail.SpecimenId,
            SpecimenCode = detail.SpecimenCode,
            SpecimenStatus = detail.SpecimenStatus,
            CollectedAtLocal = detail.CollectedAtUtc.HasValue ? ConvertUtcToClinicLocal(detail.CollectedAtUtc.Value) : null,
            ReceivedAtLocal = detail.ReceivedAtUtc.HasValue ? ConvertUtcToClinicLocal(detail.ReceivedAtUtc.Value) : null,
            Findings = detail.Findings,
            Impression = detail.Impression,
            ReportUri = detail.ReportUri,
            SignedByUsername = detail.SignedByUsername,
            SignedAtLocal = detail.SignedAtUtc.HasValue ? ConvertUtcToClinicLocal(detail.SignedAtUtc.Value) : null,
            ResultItems = detail.ResultItems.Select(x => new HospitalLabResultItemDto
            {
                ResultItemId = x.ResultItemId,
                AnalyteCode = x.AnalyteCode,
                AnalyteName = x.AnalyteName,
                ResultValue = x.ResultValue,
                Unit = x.Unit,
                ReferenceRange = x.ReferenceRange,
                AbnormalFlag = x.AbnormalFlag
            }).ToList()
        };
    }

    private static string NormalizeCategory(string? category)
    {
        var normalized = category?.Trim();
        if (string.Equals(normalized, "Lab", StringComparison.OrdinalIgnoreCase))
        {
            return "Lab";
        }

        if (string.Equals(normalized, "Imaging", StringComparison.OrdinalIgnoreCase))
        {
            return "Imaging";
        }

        throw new InvalidOperationException("Loai chi dinh khong hop le.");
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
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

    private static string GenerateOrderNumber(string category, DateTime nowUtc)
        => category == "Lab"
            ? $"ORD-LAB-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
            : $"ORD-IMG-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    private static string GenerateSpecimenCode(DateTime nowUtc)
        => $"SPC-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

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
