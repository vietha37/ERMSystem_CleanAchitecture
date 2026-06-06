using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalClinicalOrderRepository : IHospitalClinicalOrderRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalClinicalOrderRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<PaginatedResult<HospitalClinicalOrderSummaryDto>> GetWorklistAsync(
        HospitalClinicalOrderWorklistRequestDto request,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var categoryFilter = request.Category?.Trim();
        var statusFilter = request.Status?.Trim();
        var keyword = request.TextSearch?.Trim();
        var pattern = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword}%";

        var labOrders = await _hospitalDbContext.LabOrders
            .AsNoTracking()
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Patient)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader).ThenInclude(x => x.OrderedByUser)
            .Include(x => x.LabService)
            .Include(x => x.ResultItems)
            .ToListAsync(ct);

        var imagingOrders = await _hospitalDbContext.ImagingOrders
            .AsNoTracking()
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Patient)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader).ThenInclude(x => x.OrderedByUser)
            .Include(x => x.ImagingService)
            .Include(x => x.ImagingReport)
            .ToListAsync(ct);

        var combined = labOrders
            .Select(MapLabSummary)
            .Concat(imagingOrders.Select(MapImagingSummary))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            combined = combined.Where(x => string.Equals(x.Category, categoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            combined = combined.Where(x => string.Equals(x.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            combined = combined.Where(x =>
                Like(x.OrderNumber, keyword!) ||
                Like(x.PatientName, keyword!) ||
                Like(x.MedicalRecordNumber, keyword!) ||
                Like(x.DoctorName, keyword!) ||
                Like(x.ServiceName, keyword!) ||
                Like(x.EncounterNumber, keyword!));
        }

        if (request.DoctorProfileId.HasValue)
        {
            combined = combined.Where(x => x.DoctorProfileId == request.DoctorProfileId.Value);
        }

        var ordered = combined
            .OrderByDescending(x => x.RequestedAtLocal)
            .ToArray();

        var totalCount = ordered.Length;
        var items = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PaginatedResult<HospitalClinicalOrderSummaryDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<HospitalClinicalOrderDetailSnapshot?> GetByIdAsync(Guid clinicalOrderId, CancellationToken ct = default)
    {
        var labOrder = await _hospitalDbContext.LabOrders
            .AsNoTracking()
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Patient)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader).ThenInclude(x => x.OrderedByUser)
            .Include(x => x.LabService)
            .Include(x => x.Specimens)
            .Include(x => x.ResultItems)
            .FirstOrDefaultAsync(x => x.Id == clinicalOrderId, ct);

        if (labOrder != null)
        {
            return MapLabDetail(labOrder);
        }

        var imagingOrder = await _hospitalDbContext.ImagingOrders
            .AsNoTracking()
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Patient)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.OrderHeader).ThenInclude(x => x.Encounter).ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader).ThenInclude(x => x.OrderedByUser)
            .Include(x => x.ImagingService)
            .Include(x => x.ImagingReport).ThenInclude(x => x!.SignedByUser)
            .FirstOrDefaultAsync(x => x.Id == clinicalOrderId, ct);

        return imagingOrder == null ? null : MapImagingDetail(imagingOrder);
    }

    public async Task<HospitalClinicalOrderEligibleEncounterDto[]> GetEligibleEncountersAsync(Guid? doctorProfileId, CancellationToken ct = default)
    {
        var query = _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.Diagnoses)
            .Where(x => x.EncounterStatus == "InProgress" || x.EncounterStatus == "Finalized" || x.EncounterStatus == "Approved")
            .AsQueryable();

        if (doctorProfileId.HasValue)
        {
            query = query.Where(x => x.DoctorProfileId == doctorProfileId.Value);
        }

        var encounters = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return encounters.Select(x =>
        {
            var diagnosis = x.Diagnoses
                .OrderByDescending(d => d.IsPrimary)
                .ThenByDescending(d => d.NotedAtUtc)
                .FirstOrDefault();

            return new HospitalClinicalOrderEligibleEncounterDto
            {
                EncounterId = x.Id,
                EncounterNumber = x.EncounterNumber,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                MedicalRecordNumber = x.Patient.MedicalRecordNumber,
                DoctorProfileId = x.DoctorProfileId,
                DoctorName = x.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = x.DoctorProfile.Specialty.Name,
                ClinicName = x.Clinic.Name,
                EncounterStatus = x.EncounterStatus,
                PrimaryDiagnosisName = diagnosis?.DiagnosisName,
                StartedAtLocal = ConvertUtcToClinicLocal(x.StartedAtUtc)
            };
        }).ToArray();
    }

    public async Task<HospitalClinicalOrderCatalogItemDto[]> GetCatalogAsync(CancellationToken ct = default)
    {
        var lab = await _hospitalDbContext.LabServices
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new HospitalClinicalOrderCatalogItemDto
            {
                ServiceId = x.Id,
                Category = "Lab",
                ServiceCode = x.ServiceCode,
                ServiceName = x.Name,
                ExtraLabel = x.SampleType
            })
            .ToArrayAsync(ct);

        var imaging = await _hospitalDbContext.ImagingServices
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new HospitalClinicalOrderCatalogItemDto
            {
                ServiceId = x.Id,
                Category = "Imaging",
                ServiceCode = x.ServiceCode,
                ServiceName = x.Name,
                ExtraLabel = x.Modality
            })
            .ToArrayAsync(ct);

        return lab.Concat(imaging).ToArray();
    }

    public async Task<HospitalClinicalOrderEncounterSnapshot?> GetEncounterForOrderingAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == encounterId, ct);

        return encounter == null
            ? null
            : new HospitalClinicalOrderEncounterSnapshot
            {
                EncounterId = encounter.Id,
                EncounterNumber = encounter.EncounterNumber,
                EncounterStatus = encounter.EncounterStatus,
                PatientId = encounter.PatientId,
                PatientName = encounter.Patient.FullName,
                MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
                DoctorProfileId = encounter.DoctorProfileId,
                DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = encounter.DoctorProfile.Specialty.Name,
                ClinicName = encounter.Clinic.Name
            };
    }

    public async Task<HospitalClinicalOrderServiceSnapshot?> GetCatalogServiceAsync(string category, Guid serviceId, CancellationToken ct = default)
    {
        if (string.Equals(category, "Lab", StringComparison.OrdinalIgnoreCase))
        {
            return await _hospitalDbContext.LabServices
                .AsNoTracking()
                .Where(x => x.Id == serviceId && x.IsActive)
                .Select(x => new HospitalClinicalOrderServiceSnapshot
                {
                    ServiceId = x.Id,
                    Category = "Lab",
                    ServiceCode = x.ServiceCode,
                    ServiceName = x.Name
                })
                .FirstOrDefaultAsync(ct);
        }

        if (string.Equals(category, "Imaging", StringComparison.OrdinalIgnoreCase))
        {
            return await _hospitalDbContext.ImagingServices
                .AsNoTracking()
                .Where(x => x.Id == serviceId && x.IsActive)
                .Select(x => new HospitalClinicalOrderServiceSnapshot
                {
                    ServiceId = x.Id,
                    Category = "Imaging",
                    ServiceCode = x.ServiceCode,
                    ServiceName = x.Name
                })
                .FirstOrDefaultAsync(ct);
        }

        return null;
    }

    public Task AddOrderHeaderAsync(HospitalClinicalOrderHeaderCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.OrderHeaders.Add(new HospitalOrderHeaderEntity
        {
            Id = command.OrderHeaderId,
            EncounterId = command.EncounterId,
            OrderNumber = command.OrderNumber,
            OrderCategory = command.OrderCategory,
            OrderStatus = command.OrderStatus,
            OrderedByUserId = command.OrderedByUserId,
            OrderedAtUtc = command.OrderedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddLabOrderAsync(HospitalLabOrderCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.LabOrders.Add(new HospitalLabOrderEntity
        {
            Id = command.LabOrderId,
            OrderHeaderId = command.OrderHeaderId,
            LabServiceId = command.LabServiceId,
            OrderStatus = command.OrderStatus,
            PriorityCode = command.PriorityCode,
            RequestedAtUtc = command.RequestedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddImagingOrderAsync(HospitalImagingOrderCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.ImagingOrders.Add(new HospitalImagingOrderEntity
        {
            Id = command.ImagingOrderId,
            OrderHeaderId = command.OrderHeaderId,
            ImagingServiceId = command.ImagingServiceId,
            OrderStatus = command.OrderStatus,
            RequestedAtUtc = command.RequestedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddSpecimenAsync(HospitalSpecimenCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Specimens.Add(new HospitalSpecimenEntity
        {
            Id = command.SpecimenId,
            LabOrderId = command.LabOrderId,
            SpecimenCode = command.SpecimenCode,
            CollectedAtUtc = command.CollectedAtUtc,
            ReceivedAtUtc = command.ReceivedAtUtc,
            Status = command.Status
        });

        return Task.CompletedTask;
    }

    public async Task ReplaceLabResultItemsAsync(
        Guid labOrderId,
        IReadOnlyCollection<HospitalLabResultItemCreateCommand> items,
        CancellationToken ct = default)
    {
        var existing = await _hospitalDbContext.LabResultItems
            .Where(x => x.LabOrderId == labOrderId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _hospitalDbContext.LabResultItems.RemoveRange(existing);
        }

        foreach (var item in items)
        {
            _hospitalDbContext.LabResultItems.Add(new HospitalLabResultItemEntity
            {
                Id = item.ResultItemId,
                LabOrderId = item.LabOrderId,
                AnalyteCode = item.AnalyteCode,
                AnalyteName = item.AnalyteName,
                ResultValue = item.ResultValue,
                Unit = item.Unit,
                ReferenceRange = item.ReferenceRange,
                AbnormalFlag = item.AbnormalFlag,
                VerifiedAtUtc = item.VerifiedAtUtc
            });
        }
    }

    public async Task UpdateLabOrderCompletionAsync(Guid labOrderId, string status, DateTime resultedAtUtc, CancellationToken ct = default)
    {
        var labOrder = await _hospitalDbContext.LabOrders
            .FirstOrDefaultAsync(x => x.Id == labOrderId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay chi dinh xet nghiem.");

        labOrder.OrderStatus = status;
        labOrder.ResultedAtUtc = resultedAtUtc;
    }

    public Task AddImagingReportAsync(HospitalImagingReportCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.ImagingReports.Add(new HospitalImagingReportEntity
        {
            Id = command.ImagingReportId,
            ImagingOrderId = command.ImagingOrderId,
            Findings = command.Findings,
            Impression = command.Impression,
            ReportUri = command.ReportUri,
            SignedByUserId = command.SignedByUserId,
            SignedAtUtc = command.SignedAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task UpdateImagingOrderCompletionAsync(Guid imagingOrderId, string status, DateTime reportedAtUtc, CancellationToken ct = default)
    {
        var imagingOrder = await _hospitalDbContext.ImagingOrders
            .FirstOrDefaultAsync(x => x.Id == imagingOrderId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay chi dinh chan doan hinh anh.");

        imagingOrder.OrderStatus = status;
        imagingOrder.ReportedAtUtc = reportedAtUtc;
    }

    public async Task UpdateOrderHeaderStatusAsync(Guid orderHeaderId, string status, CancellationToken ct = default)
    {
        var orderHeader = await _hospitalDbContext.OrderHeaders
            .FirstOrDefaultAsync(x => x.Id == orderHeaderId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay order header.");

        orderHeader.OrderStatus = status;
    }

    public Task AddOutboxMessageAsync(HospitalClinicalOrderOutboxCreateCommand command, CancellationToken ct = default)
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

    private static HospitalClinicalOrderSummaryDto MapLabSummary(HospitalLabOrderEntity order)
    {
        var encounter = order.OrderHeader.Encounter;
        return new HospitalClinicalOrderSummaryDto
        {
            ClinicalOrderId = order.Id,
            OrderHeaderId = order.OrderHeaderId,
            OrderNumber = order.OrderHeader.OrderNumber,
            Category = "Lab",
            Status = order.OrderStatus,
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            ServiceCode = order.LabService.ServiceCode,
            ServiceName = order.LabService.Name,
            PriorityCode = order.PriorityCode,
            RequestedAtLocal = ConvertUtcToClinicLocal(order.RequestedAtUtc),
            CompletedAtLocal = order.ResultedAtUtc.HasValue ? ConvertUtcToClinicLocal(order.ResultedAtUtc.Value) : null,
            OrderedByUsername = order.OrderHeader.OrderedByUser?.Username,
            ResultItemCount = order.ResultItems.Count,
            SummaryText = order.ResultItems.Count > 0 ? "Da co ket qua xet nghiem" : null
        };
    }

    private static HospitalClinicalOrderSummaryDto MapImagingSummary(HospitalImagingOrderEntity order)
    {
        var encounter = order.OrderHeader.Encounter;
        return new HospitalClinicalOrderSummaryDto
        {
            ClinicalOrderId = order.Id,
            OrderHeaderId = order.OrderHeaderId,
            OrderNumber = order.OrderHeader.OrderNumber,
            Category = "Imaging",
            Status = order.OrderStatus,
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            ServiceCode = order.ImagingService.ServiceCode,
            ServiceName = order.ImagingService.Name,
            RequestedAtLocal = ConvertUtcToClinicLocal(order.RequestedAtUtc),
            CompletedAtLocal = order.ReportedAtUtc.HasValue ? ConvertUtcToClinicLocal(order.ReportedAtUtc.Value) : null,
            OrderedByUsername = order.OrderHeader.OrderedByUser?.Username,
            ResultItemCount = order.ImagingReport == null ? 0 : 1,
            SummaryText = order.ImagingReport?.Impression
        };
    }

    private static HospitalClinicalOrderDetailSnapshot MapLabDetail(HospitalLabOrderEntity order)
    {
        var encounter = order.OrderHeader.Encounter;
        var specimen = order.Specimens
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        return new HospitalClinicalOrderDetailSnapshot
        {
            ClinicalOrderId = order.Id,
            OrderHeaderId = order.OrderHeaderId,
            OrderNumber = order.OrderHeader.OrderNumber,
            Category = "Lab",
            Status = order.OrderStatus,
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            ServiceCode = order.LabService.ServiceCode,
            ServiceName = order.LabService.Name,
            PriorityCode = order.PriorityCode,
            RequestedAtUtc = order.RequestedAtUtc,
            CompletedAtUtc = order.ResultedAtUtc,
            OrderedByUsername = order.OrderHeader.OrderedByUser?.Username,
            SpecimenId = specimen?.Id,
            SpecimenCode = specimen?.SpecimenCode,
            SpecimenStatus = specimen?.Status,
            CollectedAtUtc = specimen?.CollectedAtUtc,
            ReceivedAtUtc = specimen?.ReceivedAtUtc,
            ResultItems = order.ResultItems
                .OrderBy(x => x.AnalyteName)
                .Select(x => new HospitalClinicalOrderLabResultItemSnapshot
                {
                    ResultItemId = x.Id,
                    AnalyteCode = x.AnalyteCode,
                    AnalyteName = x.AnalyteName,
                    ResultValue = x.ResultValue,
                    Unit = x.Unit,
                    ReferenceRange = x.ReferenceRange,
                    AbnormalFlag = x.AbnormalFlag
                })
                .ToArray()
        };
    }

    private static HospitalClinicalOrderDetailSnapshot MapImagingDetail(HospitalImagingOrderEntity order)
    {
        var encounter = order.OrderHeader.Encounter;
        return new HospitalClinicalOrderDetailSnapshot
        {
            ClinicalOrderId = order.Id,
            OrderHeaderId = order.OrderHeaderId,
            OrderNumber = order.OrderHeader.OrderNumber,
            Category = "Imaging",
            Status = order.OrderStatus,
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            ServiceCode = order.ImagingService.ServiceCode,
            ServiceName = order.ImagingService.Name,
            RequestedAtUtc = order.RequestedAtUtc,
            CompletedAtUtc = order.ReportedAtUtc,
            OrderedByUsername = order.OrderHeader.OrderedByUser?.Username,
            Findings = order.ImagingReport?.Findings,
            Impression = order.ImagingReport?.Impression,
            ReportUri = order.ImagingReport?.ReportUri,
            SignedByUsername = order.ImagingReport?.SignedByUser?.Username,
            SignedAtUtc = order.ImagingReport?.SignedAtUtc
        };
    }

    private static bool Like(string? source, string keyword)
        => !string.IsNullOrWhiteSpace(source) &&
           source.Contains(keyword, StringComparison.OrdinalIgnoreCase);

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
