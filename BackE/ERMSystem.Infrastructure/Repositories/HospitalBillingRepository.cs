using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalBillingRepository : IHospitalBillingRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalBillingRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _hospitalDbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Encounter)
            .Include(x => x.InvoiceItems)
            .Include(x => x.Payments)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.InvoiceStatus))
        {
            var status = request.InvoiceStatus.Trim();
            query = query.Where(x => x.InvoiceStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(request.TextSearch))
        {
            var pattern = $"%{request.TextSearch.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.InvoiceNumber, pattern) ||
                EF.Functions.Like(x.Patient.FullName, pattern) ||
                EF.Functions.Like(x.Patient.MedicalRecordNumber, pattern) ||
                (x.Encounter != null && EF.Functions.Like(x.Encounter.EncounterNumber, pattern)));
        }

        var totalCount = await query.CountAsync(ct);
        var invoices = await query
            .OrderByDescending(x => x.IssuedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = invoices.Select(MapSummary).ToArray();
        return new PaginatedResult<HospitalInvoiceSummaryDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<HospitalInvoiceAggregateSnapshot?> GetByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _hospitalDbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Encounter)
                .ThenInclude(x => x!.DoctorProfile)
                    .ThenInclude(x => x.StaffProfile)
            .Include(x => x.Encounter)
                .ThenInclude(x => x!.DoctorProfile)
                    .ThenInclude(x => x.Specialty)
            .Include(x => x.Encounter)
                .ThenInclude(x => x!.Clinic)
            .Include(x => x.InvoiceItems)
            .Include(x => x.Payments)
                .ThenInclude(x => x.ReceivedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct);

        return invoice == null ? null : MapAggregate(invoice);
    }

    public async Task<HospitalBillingEligibleEncounterDto[]> GetEligibleEncountersAsync(CancellationToken ct = default)
    {
        var encounters = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var encounterIds = encounters.Select(x => x.Id).ToArray();
        var existingInvoices = await _hospitalDbContext.Invoices
            .AsNoTracking()
            .Where(x => x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value))
            .Select(x => new { x.Id, x.InvoiceNumber, EncounterId = x.EncounterId!.Value })
            .ToListAsync(ct);
        var invoiceLookup = existingInvoices.ToDictionary(x => x.EncounterId, x => x);

        var labCounts = await _hospitalDbContext.LabOrders
            .AsNoTracking()
            .Where(x => encounterIds.Contains(x.OrderHeader.EncounterId) && x.OrderStatus == "Completed")
            .GroupBy(x => x.OrderHeader.EncounterId)
            .Select(x => new { EncounterId = x.Key, Count = x.Count() })
            .ToListAsync(ct);
        var labLookup = labCounts.ToDictionary(x => x.EncounterId, x => x.Count);

        var imagingCounts = await _hospitalDbContext.ImagingOrders
            .AsNoTracking()
            .Where(x => encounterIds.Contains(x.OrderHeader.EncounterId) && x.OrderStatus == "Completed")
            .GroupBy(x => x.OrderHeader.EncounterId)
            .Select(x => new { EncounterId = x.Key, Count = x.Count() })
            .ToListAsync(ct);
        var imagingLookup = imagingCounts.ToDictionary(x => x.EncounterId, x => x.Count);

        var prescriptionCounts = await _hospitalDbContext.PrescriptionItems
            .AsNoTracking()
            .Where(x => x.UnitPrice.HasValue && x.UnitPrice > 0 && encounterIds.Contains(x.Prescription.OrderHeader.EncounterId))
            .GroupBy(x => x.Prescription.OrderHeader.EncounterId)
            .Select(x => new { EncounterId = x.Key, Count = x.Count() })
            .ToListAsync(ct);
        var prescriptionLookup = prescriptionCounts.ToDictionary(x => x.EncounterId, x => x.Count);

        return encounters.Select(x =>
        {
            invoiceLookup.TryGetValue(x.Id, out var existingInvoice);
            return new HospitalBillingEligibleEncounterDto
            {
                EncounterId = x.Id,
                EncounterNumber = x.EncounterNumber,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                MedicalRecordNumber = x.Patient.MedicalRecordNumber,
                DoctorName = x.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = x.DoctorProfile.Specialty.Name,
                ClinicName = x.Clinic.Name,
                ConsultationFee = x.DoctorProfile.ConsultationFee ?? 0,
                CompletedLabOrders = labLookup.GetValueOrDefault(x.Id),
                CompletedImagingOrders = imagingLookup.GetValueOrDefault(x.Id),
                BilledPrescriptionItems = prescriptionLookup.GetValueOrDefault(x.Id),
                ExistingInvoiceId = existingInvoice?.Id,
                ExistingInvoiceNumber = existingInvoice?.InvoiceNumber,
                StartedAtLocal = ConvertUtcToClinicLocal(x.StartedAtUtc)
            };
        }).ToArray();
    }

    public async Task<HospitalBillingEncounterSnapshot?> GetEncounterForInvoiceAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == encounterId, ct);

        if (encounter == null)
        {
            return null;
        }

        var existingInvoice = await _hospitalDbContext.Invoices
            .AsNoTracking()
            .Where(x => x.EncounterId == encounterId)
            .Select(x => new { x.Id, x.InvoiceNumber })
            .FirstOrDefaultAsync(ct);

        var billableLines = new List<HospitalBillableLineSnapshot>
        {
            new()
            {
                ItemType = "Consultation",
                Description = $"Kham {encounter.DoctorProfile.Specialty.Name}",
                Quantity = 1,
                UnitPrice = encounter.DoctorProfile.ConsultationFee ?? 0,
                LineAmount = encounter.DoctorProfile.ConsultationFee ?? 0,
                ReferenceType = "Encounter",
                ReferenceId = encounter.Id
            }
        };

        var labOrders = await _hospitalDbContext.LabOrders
            .AsNoTracking()
            .Include(x => x.LabService)
            .Where(x => x.OrderHeader.EncounterId == encounterId && x.OrderStatus == "Completed")
            .ToListAsync(ct);

        billableLines.AddRange(labOrders.Select(x => new HospitalBillableLineSnapshot
        {
            ServiceCatalogId = _hospitalDbContext.ServiceCatalog
                .Where(s => s.ServiceCode == x.LabService.ServiceCode)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefault(),
            ItemType = "Laboratory",
            Description = x.LabService.Name,
            Quantity = 1,
            UnitPrice = x.LabService.UnitPrice ?? 0,
            LineAmount = x.LabService.UnitPrice ?? 0,
            ReferenceType = "LabOrder",
            ReferenceId = x.Id
        }));

        var imagingOrders = await _hospitalDbContext.ImagingOrders
            .AsNoTracking()
            .Include(x => x.ImagingService)
            .Where(x => x.OrderHeader.EncounterId == encounterId && x.OrderStatus == "Completed")
            .ToListAsync(ct);

        billableLines.AddRange(imagingOrders.Select(x => new HospitalBillableLineSnapshot
        {
            ServiceCatalogId = _hospitalDbContext.ServiceCatalog
                .Where(s => s.ServiceCode == x.ImagingService.ServiceCode)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefault(),
            ItemType = "Imaging",
            Description = x.ImagingService.Name,
            Quantity = 1,
            UnitPrice = x.ImagingService.UnitPrice ?? 0,
            LineAmount = x.ImagingService.UnitPrice ?? 0,
            ReferenceType = "ImagingOrder",
            ReferenceId = x.Id
        }));

        var prescriptionItems = await _hospitalDbContext.PrescriptionItems
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Where(x =>
                x.Prescription.OrderHeader.EncounterId == encounterId &&
                x.Prescription.Status == "Dispensed" &&
                x.UnitPrice.HasValue &&
                x.UnitPrice > 0)
            .ToListAsync(ct);

        billableLines.AddRange(prescriptionItems.Select(x => new HospitalBillableLineSnapshot
        {
            ItemType = "Medication",
            Description = x.Medicine.Name,
            Quantity = x.Quantity,
            UnitPrice = x.UnitPrice ?? 0,
            LineAmount = x.Quantity * (x.UnitPrice ?? 0),
            ReferenceType = "PrescriptionItem",
            ReferenceId = x.Id
        }));

        return new HospitalBillingEncounterSnapshot
        {
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            PatientPhone = encounter.Patient.Phone,
            PatientEmail = encounter.Patient.Email,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            ConsultationFee = encounter.DoctorProfile.ConsultationFee ?? 0,
            StartedAtUtc = encounter.StartedAtUtc,
            ExistingInvoiceId = existingInvoice?.Id,
            ExistingInvoiceNumber = existingInvoice?.InvoiceNumber,
            BillableLines = billableLines.ToArray()
        };
    }

    public async Task<HospitalBillingDashboardSnapshot> GetDashboardSnapshotAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        var invoicePaymentTotals = await _hospitalDbContext.Payments
            .AsNoTracking()
            .Where(x => x.PaymentStatus == "Captured" || x.PaymentStatus == "Refunded")
            .GroupBy(x => x.InvoiceId)
            .Select(g => new
            {
                InvoiceId = g.Key,
                PaidAmount = g.Sum(x => x.Amount)
            })
            .ToListAsync(ct);

        var paidLookup = invoicePaymentTotals.ToDictionary(x => x.InvoiceId, x => x.PaidAmount);

        var invoices = await _hospitalDbContext.Invoices
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.InvoiceStatus,
                x.TotalAmount,
                x.IssuedAtUtc
            })
            .ToListAsync(ct);

        var totalInvoices = invoices.Count;
        var paidInvoices = invoices.Count(x => x.InvoiceStatus == "Paid");
        var issuedAmountInRange = invoices
            .Where(x => x.IssuedAtUtc >= fromUtc && x.IssuedAtUtc <= toUtc)
            .Sum(x => x.TotalAmount);
        var outstandingBalanceAmount = invoices
            .Sum(x => x.TotalAmount - paidLookup.GetValueOrDefault(x.Id));

        var collectedAmountInRange = await _hospitalDbContext.Payments
            .AsNoTracking()
            .Where(x =>
                (x.PaymentStatus == "Captured" || x.PaymentStatus == "Refunded") &&
                x.PaidAtUtc.HasValue &&
                x.PaidAtUtc.Value >= fromUtc &&
                x.PaidAtUtc.Value <= toUtc)
            .SumAsync(x => x.Amount, ct);

        return new HospitalBillingDashboardSnapshot
        {
            TotalInvoices = totalInvoices,
            PaidInvoices = paidInvoices,
            IssuedAmountInRange = issuedAmountInRange,
            CollectedAmountInRange = collectedAmountInRange,
            OutstandingBalanceAmount = outstandingBalanceAmount
        };
    }

    public Task AddInvoiceAsync(HospitalInvoiceCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Invoices.Add(new HospitalInvoiceEntity
        {
            Id = command.InvoiceId,
            InvoiceNumber = command.InvoiceNumber,
            PatientId = command.PatientId,
            EncounterId = command.EncounterId,
            InvoiceStatus = command.InvoiceStatus,
            CurrencyCode = command.CurrencyCode,
            SubtotalAmount = command.SubtotalAmount,
            DiscountAmount = command.DiscountAmount,
            InsuranceAmount = command.InsuranceAmount,
            TotalAmount = command.TotalAmount,
            IssuedAtUtc = command.IssuedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddInvoiceItemAsync(HospitalInvoiceItemCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.InvoiceItems.Add(new HospitalInvoiceItemEntity
        {
            Id = command.InvoiceItemId,
            InvoiceId = command.InvoiceId,
            ServiceCatalogId = command.ServiceCatalogId,
            ItemType = command.ItemType,
            Description = command.Description,
            Quantity = command.Quantity,
            UnitPrice = command.UnitPrice,
            LineAmount = command.LineAmount,
            ReferenceType = command.ReferenceType,
            ReferenceId = command.ReferenceId
        });

        return Task.CompletedTask;
    }

    public Task AddPaymentAsync(HospitalPaymentCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Payments.Add(new HospitalPaymentEntity
        {
            Id = command.PaymentId,
            InvoiceId = command.InvoiceId,
            PaymentReference = command.PaymentReference,
            PaymentMethod = command.PaymentMethod,
            Amount = command.Amount,
            PaymentStatus = command.PaymentStatus,
            PaidAtUtc = command.PaidAtUtc,
            ReceivedByUserId = command.ReceivedByUserId,
            ExternalTransactionId = command.ExternalTransactionId
        });

        return Task.CompletedTask;
    }

    public async Task UpdateInvoiceAmountsAsync(Guid invoiceId, string invoiceStatus, decimal subtotalAmount, decimal discountAmount, decimal insuranceAmount, decimal totalAmount, CancellationToken ct = default)
    {
        var invoice = await _hospitalDbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay hoa don.");

        invoice.InvoiceStatus = invoiceStatus;
        invoice.SubtotalAmount = subtotalAmount;
        invoice.DiscountAmount = discountAmount;
        invoice.InsuranceAmount = insuranceAmount;
        invoice.TotalAmount = totalAmount;
    }

    public Task AddOutboxMessageAsync(HospitalBillingOutboxCreateCommand command, CancellationToken ct = default)
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

    private static HospitalInvoiceSummaryDto MapSummary(HospitalInvoiceEntity invoice)
    {
        var paidAmount = invoice.Payments
            .Where(x => x.PaymentStatus is "Captured" or "Refunded")
            .Sum(x => x.Amount);

        return new HospitalInvoiceSummaryDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientId = invoice.PatientId,
            PatientName = invoice.Patient.FullName,
            MedicalRecordNumber = invoice.Patient.MedicalRecordNumber,
            EncounterId = invoice.EncounterId,
            EncounterNumber = invoice.Encounter?.EncounterNumber,
            InvoiceStatus = invoice.InvoiceStatus,
            SubtotalAmount = invoice.SubtotalAmount,
            DiscountAmount = invoice.DiscountAmount,
            InsuranceAmount = invoice.InsuranceAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = paidAmount,
            BalanceAmount = invoice.TotalAmount - paidAmount,
            TotalItems = invoice.InvoiceItems.Count,
            TotalPayments = invoice.Payments.Count,
            IssuedAtLocal = ConvertUtcToClinicLocal(invoice.IssuedAtUtc),
            DueAtLocal = invoice.DueAtUtc.HasValue ? ConvertUtcToClinicLocal(invoice.DueAtUtc.Value) : null
        };
    }

    private static HospitalInvoiceAggregateSnapshot MapAggregate(HospitalInvoiceEntity invoice)
    {
        return new HospitalInvoiceAggregateSnapshot
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientId = invoice.PatientId,
            PatientName = invoice.Patient.FullName,
            MedicalRecordNumber = invoice.Patient.MedicalRecordNumber,
            PatientPhone = invoice.Patient.Phone,
            PatientEmail = invoice.Patient.Email,
            EncounterId = invoice.EncounterId,
            EncounterNumber = invoice.Encounter?.EncounterNumber,
            DoctorName = invoice.Encounter?.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = invoice.Encounter?.DoctorProfile.Specialty.Name,
            ClinicName = invoice.Encounter?.Clinic.Name,
            InvoiceStatus = invoice.InvoiceStatus,
            SubtotalAmount = invoice.SubtotalAmount,
            DiscountAmount = invoice.DiscountAmount,
            InsuranceAmount = invoice.InsuranceAmount,
            TotalAmount = invoice.TotalAmount,
            IssuedAtUtc = invoice.IssuedAtUtc,
            DueAtUtc = invoice.DueAtUtc,
            Items = invoice.InvoiceItems
                .OrderBy(x => x.Description)
                .Select(x => new HospitalInvoiceItemSnapshot
                {
                    InvoiceItemId = x.Id,
                    ServiceCatalogId = x.ServiceCatalogId,
                    ItemType = x.ItemType,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    LineAmount = x.LineAmount,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId
                })
                .ToArray(),
            Payments = invoice.Payments
                .OrderByDescending(x => x.PaidAtUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new HospitalPaymentSnapshot
                {
                    PaymentId = x.Id,
                    PaymentReference = x.PaymentReference,
                    PaymentMethod = x.PaymentMethod,
                    Amount = x.Amount,
                    PaymentStatus = x.PaymentStatus,
                    PaidAtUtc = x.PaidAtUtc,
                    ReceivedByUsername = x.ReceivedByUser?.Username,
                    ExternalTransactionId = x.ExternalTransactionId
                })
                .ToArray()
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
