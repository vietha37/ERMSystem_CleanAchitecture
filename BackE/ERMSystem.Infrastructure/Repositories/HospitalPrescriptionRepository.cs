using System;
using System.Linq;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories;

public class HospitalPrescriptionRepository : IHospitalPrescriptionRepository
{
    private readonly HospitalDbContext _hospitalDbContext;

    public HospitalPrescriptionRepository(HospitalDbContext hospitalDbContext)
    {
        _hospitalDbContext = hospitalDbContext;
    }

    public async Task<PaginatedResult<HospitalPrescriptionSummaryDto>> GetWorklistAsync(
        HospitalPrescriptionWorklistRequestDto request,
        CancellationToken ct = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _hospitalDbContext.Prescriptions
            .AsNoTracking()
            .Include(x => x.OrderHeader)
                .ThenInclude(x => x.Encounter)
                    .ThenInclude(x => x.Patient)
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
                    .ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader)
                .ThenInclude(x => x.Encounter)
                    .ThenInclude(x => x.Diagnoses)
            .Include(x => x.PrescriptionItems)
            .Include(x => x.Dispensings)
                .ThenInclude(x => x.DispensedByUser)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var normalizedStatus = request.Status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.TextSearch))
        {
            var keyword = request.TextSearch.Trim();
            var pattern = $"%{keyword}%";
            query = query.Where(x =>
                EF.Functions.Like(x.PrescriptionNumber, pattern) ||
                EF.Functions.Like(x.OrderHeader.Encounter.EncounterNumber, pattern) ||
                EF.Functions.Like(x.OrderHeader.Encounter.Patient.FullName, pattern) ||
                EF.Functions.Like(x.OrderHeader.Encounter.Patient.MedicalRecordNumber, pattern) ||
                EF.Functions.Like(x.OrderHeader.Encounter.DoctorProfile.StaffProfile.FullName, pattern) ||
                (x.Notes != null && EF.Functions.Like(x.Notes, pattern)) ||
                x.OrderHeader.Encounter.Diagnoses.Any(d => EF.Functions.Like(d.DiagnosisName, pattern)));
        }

        if (request.DoctorProfileId.HasValue)
        {
            query = query.Where(x => x.OrderHeader.Encounter.DoctorProfileId == request.DoctorProfileId.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var mapped = items.Select(x =>
        {
            var encounter = x.OrderHeader.Encounter;
            var diagnosis = encounter.Diagnoses
                .OrderByDescending(d => d.IsPrimary)
                .ThenByDescending(d => d.NotedAtUtc)
                .FirstOrDefault();
            var latestDispensing = x.Dispensings
                .OrderByDescending(d => d.DispensedAtUtc)
                .ThenByDescending(d => d.Id)
                .FirstOrDefault();

            return new HospitalPrescriptionSummaryDto
            {
                PrescriptionId = x.Id,
                PrescriptionNumber = x.PrescriptionNumber,
                Status = x.Status,
                LatestDispensingStatus = latestDispensing?.DispensingStatus,
                EncounterId = encounter.Id,
                EncounterNumber = encounter.EncounterNumber,
                PatientId = encounter.PatientId,
                PatientName = encounter.Patient.FullName,
                MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
                DoctorProfileId = encounter.DoctorProfileId,
                DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
                SpecialtyName = encounter.DoctorProfile.Specialty.Name,
                ClinicName = encounter.Clinic.Name,
                PrimaryDiagnosisName = diagnosis?.DiagnosisName,
                TotalItems = x.PrescriptionItems.Count,
                CreatedAtLocal = ConvertUtcToClinicLocal(x.CreatedAtUtc),
                DispensedAtLocal = latestDispensing?.DispensedAtUtc.HasValue == true
                    ? ConvertUtcToClinicLocal(latestDispensing.DispensedAtUtc.Value)
                    : null,
                DispensedByUsername = latestDispensing?.DispensedByUser?.Username,
                Notes = x.Notes
            };
        }).ToArray();

        return new PaginatedResult<HospitalPrescriptionSummaryDto>(mapped, totalCount, pageNumber, pageSize);
    }

    public async Task<HospitalPrescriptionAggregateSnapshot?> GetByIdAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var entity = await _hospitalDbContext.Prescriptions
            .AsNoTracking()
            .Include(x => x.OrderHeader)
                .ThenInclude(x => x.Encounter)
                    .ThenInclude(x => x.Patient)
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
                    .ThenInclude(x => x.Clinic)
            .Include(x => x.OrderHeader)
                .ThenInclude(x => x.Encounter)
                    .ThenInclude(x => x.Diagnoses)
            .Include(x => x.PrescriptionItems)
                .ThenInclude(x => x.Medicine)
            .Include(x => x.Dispensings)
                .ThenInclude(x => x.DispensedByUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == prescriptionId, ct);

        if (entity == null)
        {
            return null;
        }

        var encounter = entity.OrderHeader.Encounter;
        var diagnosis = encounter.Diagnoses
            .OrderByDescending(d => d.IsPrimary)
            .ThenByDescending(d => d.NotedAtUtc)
            .FirstOrDefault();
        var latestDispensing = entity.Dispensings
            .OrderByDescending(d => d.DispensedAtUtc)
            .ThenByDescending(d => d.Id)
            .FirstOrDefault();

        return new HospitalPrescriptionAggregateSnapshot
        {
            PrescriptionId = entity.Id,
            OrderHeaderId = entity.OrderHeaderId,
            PrescriptionNumber = entity.PrescriptionNumber,
            Status = entity.Status,
            LatestDispensingId = latestDispensing?.Id,
            LatestDispensingStatus = latestDispensing?.DispensingStatus,
            DispensedAtUtc = latestDispensing?.DispensedAtUtc,
            DispensedByUserId = latestDispensing?.DispensedByUserId,
            DispensedByUsername = latestDispensing?.DispensedByUser?.Username,
            DispensingNotes = latestDispensing?.Notes,
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            PatientDateOfBirth = encounter.Patient.DateOfBirth,
            PatientGender = encounter.Patient.Gender,
            PatientPhone = encounter.Patient.Phone,
            PatientEmail = encounter.Patient.Email,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            PrimaryDiagnosisName = diagnosis?.DiagnosisName,
            DiagnosisNames = encounter.Diagnoses
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.NotedAtUtc)
                .Select(x => x.DiagnosisName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            CreatedAtUtc = entity.CreatedAtUtc,
            Notes = entity.Notes,
            DispensingHistory = entity.Dispensings
                .OrderByDescending(x => x.DispensedAtUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new HospitalPrescriptionDispensingSnapshot
                {
                    DispensingId = x.Id,
                    DispensingStatus = x.DispensingStatus,
                    DispensedAtUtc = x.DispensedAtUtc,
                    DispensedByUserId = x.DispensedByUserId,
                    DispensedByUsername = x.DispensedByUser?.Username,
                    Notes = x.Notes
                })
                .ToArray(),
            Items = entity.PrescriptionItems
                .OrderBy(x => x.Medicine.Name)
                .Select(x => new HospitalPrescriptionItemSnapshot
                {
                    PrescriptionItemId = x.Id,
                    MedicineId = x.MedicineId,
                    DrugCode = x.Medicine.DrugCode,
                    MedicineName = x.Medicine.Name,
                    GenericName = x.Medicine.GenericName,
                    Strength = x.Medicine.Strength,
                    DosageForm = x.Medicine.DosageForm,
                    Unit = x.Medicine.Unit,
                    DoseInstruction = x.DoseInstruction,
                    Route = x.Route,
                    Frequency = x.Frequency,
                    DurationDays = x.DurationDays,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                })
                .ToArray()
        };
    }

    public async Task<HospitalPrescriptionEligibleEncounterDto[]> GetEligibleEncountersAsync(Guid? doctorProfileId, CancellationToken ct = default)
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

        var encounterIds = encounters.Select(x => x.Id).ToArray();
        var prescriptions = await _hospitalDbContext.Prescriptions
            .AsNoTracking()
            .Where(x => encounterIds.Contains(x.OrderHeader.EncounterId))
            .Select(x => new { x.Id, x.PrescriptionNumber, EncounterId = x.OrderHeader.EncounterId })
            .ToListAsync(ct);

        var prescriptionLookup = prescriptions.ToDictionary(x => x.EncounterId, x => x);

        return encounters.Select(x =>
        {
            prescriptionLookup.TryGetValue(x.Id, out var existingPrescription);
            var diagnosis = x.Diagnoses
                .OrderByDescending(d => d.IsPrimary)
                .ThenByDescending(d => d.NotedAtUtc)
                .FirstOrDefault();

            return new HospitalPrescriptionEligibleEncounterDto
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
                StartedAtLocal = ConvertUtcToClinicLocal(x.StartedAtUtc),
                ExistingPrescriptionId = existingPrescription?.Id,
                ExistingPrescriptionNumber = existingPrescription?.PrescriptionNumber
            };
        }).ToArray();
    }

    public Task<HospitalMedicineCatalogDto[]> GetMedicineCatalogAsync(CancellationToken ct = default)
    {
        return _hospitalDbContext.Medicines
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new HospitalMedicineCatalogDto
            {
                MedicineId = x.Id,
                DrugCode = x.DrugCode,
                Name = x.Name,
                GenericName = x.GenericName,
                Strength = x.Strength,
                DosageForm = x.DosageForm,
                Unit = x.Unit,
                IsControlled = x.IsControlled
            })
            .ToArrayAsync(ct);
    }

    public async Task<HospitalPrescriptionEncounterSnapshot?> GetEncounterForPrescriptionAsync(Guid encounterId, CancellationToken ct = default)
    {
        var encounter = await _hospitalDbContext.Encounters
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile).ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .Include(x => x.Diagnoses)
            .FirstOrDefaultAsync(x => x.Id == encounterId, ct);

        if (encounter == null)
        {
            return null;
        }

        var existingPrescription = await _hospitalDbContext.Prescriptions
            .AsNoTracking()
            .Where(x => x.OrderHeader.EncounterId == encounterId)
            .Select(x => new { x.Id, x.PrescriptionNumber })
            .FirstOrDefaultAsync(ct);

        var diagnosis = encounter.Diagnoses
            .OrderByDescending(d => d.IsPrimary)
            .ThenByDescending(d => d.NotedAtUtc)
            .FirstOrDefault();

        return new HospitalPrescriptionEncounterSnapshot
        {
            EncounterId = encounter.Id,
            EncounterNumber = encounter.EncounterNumber,
            EncounterStatus = encounter.EncounterStatus,
            PatientId = encounter.PatientId,
            PatientName = encounter.Patient.FullName,
            MedicalRecordNumber = encounter.Patient.MedicalRecordNumber,
            PatientPhone = encounter.Patient.Phone,
            PatientEmail = encounter.Patient.Email,
            DoctorProfileId = encounter.DoctorProfileId,
            DoctorName = encounter.DoctorProfile.StaffProfile.FullName,
            SpecialtyName = encounter.DoctorProfile.Specialty.Name,
            ClinicName = encounter.Clinic.Name,
            PrimaryDiagnosisName = diagnosis?.DiagnosisName,
            ExistingPrescriptionId = existingPrescription?.Id,
            ExistingPrescriptionNumber = existingPrescription?.PrescriptionNumber
        };
    }

    public Task<HospitalMedicineSnapshot[]> GetMedicinesByIdsAsync(Guid[] medicineIds, CancellationToken ct = default)
    {
        return _hospitalDbContext.Medicines
            .AsNoTracking()
            .Where(x => medicineIds.Contains(x.Id) && x.IsActive)
            .Select(x => new HospitalMedicineSnapshot
            {
                MedicineId = x.Id,
                DrugCode = x.DrugCode,
                Name = x.Name,
                GenericName = x.GenericName,
                Strength = x.Strength,
                DosageForm = x.DosageForm,
                Unit = x.Unit,
                IsControlled = x.IsControlled
            })
            .ToArrayAsync(ct);
    }

    public Task<bool> HospitalUserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return _hospitalDbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId, ct);
    }

    public Task AddOrderHeaderAsync(HospitalPrescriptionOrderHeaderCreateCommand command, CancellationToken ct = default)
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

    public Task AddPrescriptionAsync(HospitalPrescriptionCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Prescriptions.Add(new HospitalPrescriptionEntity
        {
            Id = command.PrescriptionId,
            OrderHeaderId = command.OrderHeaderId,
            PrescriptionNumber = command.PrescriptionNumber,
            Status = command.Status,
            Notes = command.Notes,
            CreatedAtUtc = command.CreatedAtUtc
        });

        return Task.CompletedTask;
    }

    public Task AddPrescriptionItemAsync(HospitalPrescriptionItemCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.PrescriptionItems.Add(new HospitalPrescriptionItemEntity
        {
            Id = command.PrescriptionItemId,
            PrescriptionId = command.PrescriptionId,
            MedicineId = command.MedicineId,
            DoseInstruction = command.DoseInstruction,
            Route = command.Route,
            Frequency = command.Frequency,
            DurationDays = command.DurationDays,
            Quantity = command.Quantity,
            UnitPrice = command.UnitPrice
        });

        return Task.CompletedTask;
    }

    public Task AddDispensingAsync(HospitalPrescriptionDispensingCreateCommand command, CancellationToken ct = default)
    {
        _hospitalDbContext.Dispensings.Add(new HospitalDispensingEntity
        {
            Id = command.DispensingId,
            PrescriptionId = command.PrescriptionId,
            DispensingStatus = command.DispensingStatus,
            DispensedAtUtc = command.DispensedAtUtc,
            DispensedByUserId = command.DispensedByUserId,
            Notes = command.Notes
        });

        return Task.CompletedTask;
    }

    public async Task UpdatePrescriptionStatusAsync(Guid prescriptionId, string status, CancellationToken ct = default)
    {
        var prescription = await _hospitalDbContext.Prescriptions.FirstOrDefaultAsync(x => x.Id == prescriptionId, ct);
        if (prescription == null)
        {
            throw new KeyNotFoundException("Khong tim thay don thuoc.");
        }

        prescription.Status = status;
    }

    public async Task UpdateOrderHeaderStatusAsync(Guid orderHeaderId, string status, CancellationToken ct = default)
    {
        var orderHeader = await _hospitalDbContext.OrderHeaders.FirstOrDefaultAsync(x => x.Id == orderHeaderId, ct);
        if (orderHeader == null)
        {
            throw new KeyNotFoundException("Khong tim thay lenh chi dinh don thuoc.");
        }

        orderHeader.OrderStatus = status;
    }

    public Task AddOutboxMessageAsync(HospitalPrescriptionOutboxCreateCommand command, CancellationToken ct = default)
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

    public async Task DeletePrescriptionAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _hospitalDbContext.Prescriptions
            .Include(x => x.PrescriptionItems)
            .Include(x => x.Dispensings)
            .FirstOrDefaultAsync(x => x.Id == prescriptionId, ct);

        if (prescription == null)
        {
            throw new KeyNotFoundException("Khong tim thay don thuoc.");
        }

        if (prescription.Dispensings.Count > 0)
        {
            throw new InvalidOperationException("Don thuoc da co phat thuoc, khong the xoa.");
        }

        _hospitalDbContext.PrescriptionItems.RemoveRange(prescription.PrescriptionItems);
        _hospitalDbContext.Prescriptions.Remove(prescription);
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
}
