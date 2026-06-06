using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class HospitalPatientPortalRepository : IHospitalPatientPortalRepository
    {
        private readonly HospitalDbContext _hospitalDbContext;

        public HospitalPatientPortalRepository(HospitalDbContext hospitalDbContext)
        {
            _hospitalDbContext = hospitalDbContext;
        }

        public async Task<HospitalPatientPortalOverviewDto?> GetOverviewByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var account = await GetPortalAccountAsync(userId, ct);

            if (account == null)
            {
                return null;
            }

            var appointments = await _hospitalDbContext.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId == account.PatientId)
                .Include(x => x.DoctorProfile)
                    .ThenInclude(x => x.StaffProfile)
                .Include(x => x.DoctorProfile)
                    .ThenInclude(x => x.Specialty)
                .Include(x => x.Clinic)
                .OrderByDescending(x => x.AppointmentStartUtc)
                .Select(x => new HospitalPatientPortalAppointmentDto
                {
                    AppointmentId = x.Id,
                    AppointmentNumber = x.AppointmentNumber,
                    Status = x.Status,
                    AppointmentType = x.AppointmentType,
                    BookingChannel = x.BookingChannel,
                    AppointmentStartLocal = ConvertUtcToClinicLocal(x.AppointmentStartUtc),
                    AppointmentEndLocal = x.AppointmentEndUtc.HasValue
                        ? ConvertUtcToClinicLocal(x.AppointmentEndUtc.Value)
                        : null,
                    DoctorName = x.DoctorProfile.StaffProfile.FullName,
                    SpecialtyName = x.DoctorProfile.Specialty.Name,
                    ClinicName = x.Clinic.Name,
                    ChiefComplaint = x.ChiefComplaint
                })
                .ToListAsync(ct);

            var prescriptions = await _hospitalDbContext.Prescriptions
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => x.OrderHeader.Encounter.PatientId == account.PatientId)
                .Include(x => x.OrderHeader)
                    .ThenInclude(x => x.Encounter)
                        .ThenInclude(x => x.DoctorProfile)
                            .ThenInclude(x => x.StaffProfile)
                .Include(x => x.OrderHeader)
                    .ThenInclude(x => x.Encounter)
                        .ThenInclude(x => x.DoctorProfile)
                            .ThenInclude(x => x.Specialty)
                .Include(x => x.OrderHeader)
                    .ThenInclude(x => x.Encounter)
                        .ThenInclude(x => x.Diagnoses)
                .Include(x => x.PrescriptionItems)
                    .ThenInclude(x => x.Medicine)
                .Include(x => x.Dispensings)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(5)
                .Select(x => new HospitalPatientPortalPrescriptionDto
                {
                    PrescriptionId = x.Id,
                    PrescriptionNumber = x.PrescriptionNumber,
                    Status = x.Status,
                    EncounterNumber = x.OrderHeader.Encounter.EncounterNumber,
                    DoctorName = x.OrderHeader.Encounter.DoctorProfile.StaffProfile.FullName,
                    SpecialtyName = x.OrderHeader.Encounter.DoctorProfile.Specialty.Name,
                    PrimaryDiagnosisName = x.OrderHeader.Encounter.Diagnoses
                        .Where(d => d.IsPrimary)
                        .OrderByDescending(d => d.NotedAtUtc)
                        .Select(d => d.DiagnosisName)
                        .FirstOrDefault(),
                    TotalItems = x.PrescriptionItems.Count,
                    CreatedAtLocal = ConvertUtcToClinicLocal(x.CreatedAtUtc),
                    DispensedAtLocal = x.Dispensings
                        .OrderByDescending(d => d.DispensedAtUtc)
                        .Select(d => d.DispensedAtUtc.HasValue
                            ? ConvertUtcToClinicLocal(d.DispensedAtUtc.Value)
                            : (DateTime?)null)
                        .FirstOrDefault(),
                    Notes = x.Notes,
                    Items = x.PrescriptionItems
                        .OrderBy(i => i.Medicine.Name)
                        .Select(i => new HospitalPatientPortalPrescriptionItemDto
                        {
                            PrescriptionItemId = i.Id,
                            MedicineName = i.Medicine.Name,
                            DrugCode = i.Medicine.DrugCode,
                            DoseInstruction = i.DoseInstruction,
                            Route = i.Route,
                            Frequency = i.Frequency,
                            DurationDays = i.DurationDays,
                            Quantity = i.Quantity,
                            Unit = i.Medicine.Unit
                        })
                        .ToArray()
                })
                .ToListAsync(ct);

            var clinicalOrders = await _hospitalDbContext.OrderHeaders
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => x.Encounter.PatientId == account.PatientId)
                .Where(x => x.OrderCategory == "Lab" || x.OrderCategory == "Imaging")
                .Include(x => x.Encounter)
                    .ThenInclude(x => x.DoctorProfile)
                        .ThenInclude(x => x.StaffProfile)
                .Include(x => x.LabOrders)
                    .ThenInclude(x => x.LabService)
                .Include(x => x.LabOrders)
                    .ThenInclude(x => x.ResultItems)
                .Include(x => x.ImagingOrders)
                    .ThenInclude(x => x.ImagingService)
                .Include(x => x.ImagingOrders)
                    .ThenInclude(x => x.ImagingReport)
                .OrderByDescending(x => x.OrderedAtUtc)
                .Take(5)
                .Select(x => new HospitalPatientPortalClinicalOrderDto
                {
                    ClinicalOrderId = x.OrderCategory == "Lab"
                        ? x.LabOrders.Select(o => o.Id).FirstOrDefault()
                        : x.ImagingOrders.Select(o => o.Id).FirstOrDefault(),
                    OrderNumber = x.OrderNumber,
                    Category = x.OrderCategory,
                    Status = x.OrderStatus,
                    EncounterNumber = x.Encounter.EncounterNumber,
                    ServiceName = x.OrderCategory == "Lab"
                        ? x.LabOrders.Select(o => o.LabService.Name).FirstOrDefault() ?? string.Empty
                        : x.ImagingOrders.Select(o => o.ImagingService.Name).FirstOrDefault() ?? string.Empty,
                    ServiceCode = x.OrderCategory == "Lab"
                        ? x.LabOrders.Select(o => o.LabService.ServiceCode).FirstOrDefault() ?? string.Empty
                        : x.ImagingOrders.Select(o => o.ImagingService.ServiceCode).FirstOrDefault() ?? string.Empty,
                    DoctorName = x.Encounter.DoctorProfile.StaffProfile.FullName,
                    RequestedAtLocal = ConvertUtcToClinicLocal(x.OrderedAtUtc),
                    CompletedAtLocal = x.OrderCategory == "Lab"
                        ? x.LabOrders.Select(o => o.ResultedAtUtc)
                            .Where(o => o.HasValue)
                            .OrderByDescending(o => o)
                            .Select(o => o.HasValue ? ConvertUtcToClinicLocal(o.Value) : (DateTime?)null)
                            .FirstOrDefault()
                        : x.ImagingOrders.Select(o => o.ReportedAtUtc)
                            .Where(o => o.HasValue)
                            .OrderByDescending(o => o)
                            .Select(o => o.HasValue ? ConvertUtcToClinicLocal(o.Value) : (DateTime?)null)
                            .FirstOrDefault(),
                    SummaryText = x.OrderCategory == "Lab"
                        ? x.LabOrders.SelectMany(o => o.ResultItems)
                            .OrderBy(r => r.AnalyteName)
                            .Select(r => string.IsNullOrWhiteSpace(r.ResultValue)
                                ? r.AnalyteName
                                : $"{r.AnalyteName}: {r.ResultValue}")
                            .FirstOrDefault()
                        : x.ImagingOrders.Select(o => o.ImagingReport != null
                            ? (o.ImagingReport.Impression ?? o.ImagingReport.Findings)
                            : null).FirstOrDefault(),
                    Findings = x.ImagingOrders
                        .Select(o => o.ImagingReport != null ? o.ImagingReport.Findings : null)
                        .FirstOrDefault(),
                    Impression = x.ImagingOrders
                        .Select(o => o.ImagingReport != null ? o.ImagingReport.Impression : null)
                        .FirstOrDefault(),
                    ReportUri = x.ImagingOrders
                        .Select(o => o.ImagingReport != null ? o.ImagingReport.ReportUri : null)
                        .FirstOrDefault(),
                    ResultItems = x.LabOrders
                        .SelectMany(o => o.ResultItems)
                        .OrderBy(r => r.AnalyteName)
                        .Select(r => new HospitalPatientPortalLabResultItemDto
                        {
                            ResultItemId = r.Id,
                            AnalyteName = r.AnalyteName,
                            ResultValue = r.ResultValue,
                            Unit = r.Unit,
                            ReferenceRange = r.ReferenceRange,
                            AbnormalFlag = r.AbnormalFlag
                        })
                        .ToArray()
                })
                .ToListAsync(ct);

            var invoices = await _hospitalDbContext.Invoices
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => x.PatientId == account.PatientId)
                .Include(x => x.Encounter)
                .Include(x => x.InvoiceItems)
                .Include(x => x.Payments)
                .OrderByDescending(x => x.IssuedAtUtc)
                .Take(5)
                .Select(x => new HospitalPatientPortalInvoiceDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceStatus = x.InvoiceStatus,
                    EncounterNumber = x.Encounter != null ? x.Encounter.EncounterNumber : null,
                    TotalAmount = x.TotalAmount,
                    PaidAmount = x.Payments
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    BalanceAmount = x.TotalAmount - (x.Payments
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m),
                    TotalItems = x.InvoiceItems.Count,
                    TotalPayments = x.Payments.Count,
                    IssuedAtLocal = ConvertUtcToClinicLocal(x.IssuedAtUtc),
                    DueAtLocal = x.DueAtUtc.HasValue ? ConvertUtcToClinicLocal(x.DueAtUtc.Value) : null,
                    Items = x.InvoiceItems
                        .OrderBy(i => i.Description)
                        .Select(i => new HospitalPatientPortalInvoiceItemDto
                        {
                            InvoiceItemId = i.Id,
                            ItemType = i.ItemType,
                            Description = i.Description,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineAmount = i.LineAmount
                        })
                        .ToArray(),
                    Payments = x.Payments
                        .OrderByDescending(p => p.PaidAtUtc)
                        .Select(p => new HospitalPatientPortalPaymentDto
                        {
                            PaymentId = p.Id,
                            PaymentReference = p.PaymentReference,
                            PaymentMethod = p.PaymentMethod,
                            Amount = p.Amount,
                            PaymentStatus = p.PaymentStatus,
                            PaidAtLocal = p.PaidAtUtc.HasValue ? ConvertUtcToClinicLocal(p.PaidAtUtc.Value) : null
                        })
                        .ToArray()
                })
                .ToListAsync(ct);

            var nowLocal = ConvertUtcToClinicLocal(DateTime.UtcNow);

            return new HospitalPatientPortalOverviewDto
            {
                Profile = new HospitalPatientPortalProfileDto
                {
                    PatientId = account.Patient.Id,
                    MedicalRecordNumber = account.Patient.MedicalRecordNumber,
                    FullName = account.Patient.FullName,
                    DateOfBirth = account.Patient.DateOfBirth,
                    Gender = account.Patient.Gender,
                    Phone = account.Patient.Phone,
                    Email = account.Patient.Email,
                    Address = BuildAddress(account.Patient.AddressLine1, account.Patient.Ward, account.Patient.District, account.Patient.Province),
                    PortalStatus = account.PortalStatus,
                    ActivatedAtUtc = account.ActivatedAtUtc
                },
                UpcomingAppointments = appointments
                    .Where(x => x.AppointmentStartLocal >= nowLocal)
                    .OrderBy(x => x.AppointmentStartLocal)
                    .Take(5)
                    .ToArray(),
                RecentAppointments = appointments
                    .Where(x => x.AppointmentStartLocal < nowLocal)
                    .OrderByDescending(x => x.AppointmentStartLocal)
                    .Take(5)
                    .ToArray(),
                RecentPrescriptions = prescriptions,
                RecentClinicalOrders = clinicalOrders,
                RecentInvoices = invoices
            };
        }

        public async Task<HospitalPatientVisitHistoryResultDto?> GetVisitHistoryByUserIdAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            var account = await GetPortalAccountAsync(userId, ct);
            if (account == null)
            {
                return null;
            }

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 20);

            var baseQuery = _hospitalDbContext.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId == account.PatientId);

            var totalCount = await baseQuery.CountAsync(ct);
            var items = await baseQuery
                .OrderByDescending(x => x.AppointmentStartUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new HospitalPatientVisitHistoryItemDto
                {
                    AppointmentId = x.Id,
                    AppointmentNumber = x.AppointmentNumber,
                    AppointmentStatus = x.Status,
                    AppointmentType = x.AppointmentType,
                    BookingChannel = x.BookingChannel,
                    AppointmentStartLocal = ConvertUtcToClinicLocal(x.AppointmentStartUtc),
                    AppointmentEndLocal = x.AppointmentEndUtc.HasValue
                        ? ConvertUtcToClinicLocal(x.AppointmentEndUtc.Value)
                        : null,
                    CheckInTimeLocal = x.CheckIn != null
                        ? ConvertUtcToClinicLocal(x.CheckIn.CheckInTimeUtc)
                        : null,
                    DoctorName = x.DoctorProfile.StaffProfile.FullName,
                    SpecialtyName = x.DoctorProfile.Specialty.Name,
                    ClinicName = x.Clinic.Name,
                    ChiefComplaint = x.ChiefComplaint,
                    EncounterId = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => (Guid?)e.Id)
                        .FirstOrDefault(),
                    EncounterNumber = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => e.EncounterNumber)
                        .FirstOrDefault(),
                    EncounterStatus = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => e.EncounterStatus)
                        .FirstOrDefault(),
                    EncounterStartedLocal = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => (DateTime?)ConvertUtcToClinicLocal(e.StartedAtUtc))
                        .FirstOrDefault(),
                    EncounterEndedLocal = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => e.EndedAtUtc.HasValue
                            ? ConvertUtcToClinicLocal(e.EndedAtUtc.Value)
                            : (DateTime?)null)
                        .FirstOrDefault(),
                    PrimaryDiagnosisName = _hospitalDbContext.Diagnoses
                        .Where(d => d.Encounter.AppointmentId == x.Id)
                        .Where(d => d.IsPrimary)
                        .OrderByDescending(d => d.NotedAtUtc)
                        .Select(d => d.DiagnosisName)
                        .FirstOrDefault(),
                    ClinicalSummary = _hospitalDbContext.Encounters
                        .Where(e => e.AppointmentId == x.Id)
                        .OrderByDescending(e => e.StartedAtUtc)
                        .Select(e => e.Summary)
                        .FirstOrDefault(),
                    PrescriptionCount = _hospitalDbContext.Prescriptions
                        .Count(p => p.OrderHeader.Encounter.AppointmentId == x.Id),
                    ClinicalOrderCount = _hospitalDbContext.OrderHeaders
                        .Count(o => o.Encounter.AppointmentId == x.Id &&
                                    (o.OrderCategory == "Lab" || o.OrderCategory == "Imaging")),
                    InvoiceCount = _hospitalDbContext.Invoices
                        .Count(i => i.Encounter != null && i.Encounter.AppointmentId == x.Id),
                    TotalInvoiceAmount = _hospitalDbContext.Invoices
                        .Where(i => i.Encounter != null && i.Encounter.AppointmentId == x.Id)
                        .Sum(i => (decimal?)i.TotalAmount) ?? 0m,
                    TotalPaidAmount = _hospitalDbContext.Invoices
                        .Where(i => i.Encounter != null && i.Encounter.AppointmentId == x.Id)
                        .SelectMany(i => i.Payments)
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    OutstandingBalanceAmount = (_hospitalDbContext.Invoices
                        .Where(i => i.Encounter != null && i.Encounter.AppointmentId == x.Id)
                        .Sum(i => (decimal?)i.TotalAmount) ?? 0m) - (_hospitalDbContext.Invoices
                        .Where(i => i.Encounter != null && i.Encounter.AppointmentId == x.Id)
                        .SelectMany(i => i.Payments)
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .ToListAsync(ct);

            return new HospitalPatientVisitHistoryResultDto
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<HospitalPatientPortalInvoicePaymentAccessDto?> GetInvoicePaymentAccessAsync(
            Guid userId,
            Guid invoiceId,
            CancellationToken ct = default)
        {
            return await _hospitalDbContext.Invoices
                .AsNoTracking()
                .Where(x => x.Id == invoiceId)
                .Where(x => x.Patient.DeletedAtUtc == null)
                .Where(x => x.Patient.PatientAccount != null &&
                            x.Patient.PatientAccount.UserId == userId &&
                            x.Patient.PatientAccount.PortalStatus == "Active")
                .Select(x => new HospitalPatientPortalInvoicePaymentAccessDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceStatus = x.InvoiceStatus,
                    TotalAmount = x.TotalAmount,
                    PaidAmount = x.Payments
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m,
                    BalanceAmount = x.TotalAmount - (x.Payments
                        .Where(p => p.PaymentStatus == "Captured")
                        .Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .FirstOrDefaultAsync(ct);
        }

        private async Task<PortalAccountProjection?> GetPortalAccountAsync(Guid userId, CancellationToken ct)
        {
            return await _hospitalDbContext.PatientAccounts
                .AsNoTracking()
                .Include(x => x.Patient)
                .Where(x => x.UserId == userId)
                .Where(x => x.PortalStatus == "Active")
                .Where(x => x.Patient.DeletedAtUtc == null)
                .Select(x => new PortalAccountProjection
                {
                    PatientId = x.PatientId,
                    PortalStatus = x.PortalStatus,
                    ActivatedAtUtc = x.ActivatedAtUtc,
                    Patient = x.Patient
                })
                .FirstOrDefaultAsync(ct);
        }

        private sealed class PortalAccountProjection
        {
            public Guid PatientId { get; set; }
            public string PortalStatus { get; set; } = string.Empty;
            public DateTime ActivatedAtUtc { get; set; }
            public required ERMSystem.Infrastructure.HospitalData.Entities.HospitalPatientEntity Patient { get; set; }
        }

        private static string? BuildAddress(string? addressLine1, string? ward, string? district, string? province)
        {
            var merged = string.Join(", ", new[] { addressLine1, ward, district, province }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            return string.IsNullOrWhiteSpace(merged) ? null : merged;
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
}
