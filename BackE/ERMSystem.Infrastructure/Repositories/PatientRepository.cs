using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly HospitalDbContext _context;

        public PatientRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Patient>> GetAllAsync(CancellationToken ct = default)
        {
            var rows = await BuildPatientQuery()
                .OrderBy(x => x.Patient.CreatedAtUtc)
                .ToListAsync(ct);

            return rows.Select(MapPatient).ToList();
        }

        public async Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? textSearch = null,
            CancellationToken ct = default)
        {
            var query = BuildPatientQuery();

            if (!string.IsNullOrWhiteSpace(textSearch))
            {
                var keyword = textSearch.Trim();
                var pattern = $"%{keyword}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Patient.FullName, pattern) ||
                    (p.Patient.Phone != null && EF.Functions.Like(p.Patient.Phone, pattern)) ||
                    (p.Patient.AddressLine1 != null && EF.Functions.Like(p.Patient.AddressLine1, pattern)) ||
                    (p.Patient.AddressLine2 != null && EF.Functions.Like(p.Patient.AddressLine2, pattern)) ||
                    (p.Patient.Ward != null && EF.Functions.Like(p.Patient.Ward, pattern)) ||
                    (p.Patient.District != null && EF.Functions.Like(p.Patient.District, pattern)) ||
                    (p.Patient.Province != null && EF.Functions.Like(p.Patient.Province, pattern)) ||
                    EF.Functions.Like(p.Patient.Gender, pattern) ||
                    (p.EmergencyContact != null && EF.Functions.Like(p.EmergencyContact.FullName, pattern)) ||
                    (p.EmergencyContact != null && EF.Functions.Like(p.EmergencyContact.Phone, pattern)) ||
                    (p.EmergencyContact != null && EF.Functions.Like(p.EmergencyContact.Relationship, pattern)));
            }

            var totalCount = await query.CountAsync(ct);
            var rows = await query
                .OrderBy(x => x.Patient.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (rows.Select(MapPatient).ToList(), totalCount);
        }

        public Task<int> GetTotalCountAsync(CancellationToken ct = default)
            => _context.Patients.CountAsync(x => x.DeletedAtUtc == null, ct);

        public async Task<Dictionary<DateTime, int>> GetCreatedCountByDayAsync(DateTime fromUtc, CancellationToken ct = default)
        {
            return await _context.Patients
                .AsNoTracking()
                .Where(p => p.DeletedAtUtc == null && p.CreatedAtUtc >= fromUtc)
                .GroupBy(p => p.CreatedAtUtc.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        }

        public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var row = await BuildPatientQuery()
                .FirstOrDefaultAsync(x => x.Patient.Id == id, ct);

            return row == null ? null : MapPatient(row);
        }

        public async Task<Patient?> GetByAppUserIdAsync(Guid appUserId, CancellationToken ct = default)
        {
            var row = await BuildPatientQuery()
                .FirstOrDefaultAsync(x => x.AppUserId == appUserId, ct);

            return row == null ? null : MapPatient(row);
        }

        public Task<int> GetAppointmentCountAsync(Guid patientId, CancellationToken ct = default)
            => _context.Appointments.CountAsync(a => a.PatientId == patientId, ct);

        public async Task<IReadOnlyCollection<Patient>> GetPotentialDuplicateCandidatesAsync(
            Guid patientId,
            string fullName,
            DateTime dateOfBirth,
            string phone,
            string? emergencyContactPhone,
            CancellationToken ct = default)
        {
            var dob = DateOnly.FromDateTime(dateOfBirth);

            var query = BuildPatientQuery()
                .Where(x => x.Patient.Id != patientId)
                .Where(x =>
                    x.Patient.Phone == phone ||
                    (x.Patient.FullName == fullName && x.Patient.DateOfBirth == dob) ||
                    (!string.IsNullOrWhiteSpace(emergencyContactPhone) &&
                     x.EmergencyContact != null &&
                     x.EmergencyContact.Phone == emergencyContactPhone));

            var rows = await query
                .OrderBy(x => x.Patient.FullName)
                .ThenBy(x => x.Patient.CreatedAtUtc)
                .ToListAsync(ct);

            return rows.Select(MapPatient).ToList();
        }

        public async Task<int> ReassignAppointmentsAsync(Guid sourcePatientId, Guid targetPatientId, CancellationToken ct = default)
        {
            var appointments = await _context.Appointments
                .Where(a => a.PatientId == sourcePatientId)
                .ToListAsync(ct);

            foreach (var appointment in appointments)
            {
                appointment.PatientId = targetPatientId;
                appointment.UpdatedAtUtc = DateTime.UtcNow;
            }

            var encounters = await _context.Encounters
                .Where(e => e.PatientId == sourcePatientId)
                .ToListAsync(ct);

            foreach (var encounter in encounters)
            {
                encounter.PatientId = targetPatientId;
                encounter.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
            return appointments.Count;
        }

        public async Task AddAsync(Patient patient, CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;
            var entity = new HospitalPatientEntity
            {
                Id = patient.Id,
                MedicalRecordNumber = $"MRN-{patient.Id.ToString("N")[..8].ToUpperInvariant()}",
                FullName = patient.FullName.Trim(),
                DateOfBirth = DateOnly.FromDateTime(patient.DateOfBirth),
                Gender = patient.Gender.Trim(),
                Phone = patient.Phone.Trim(),
                AddressLine1 = patient.Address.Trim(),
                CreatedAtUtc = patient.CreatedAt == default ? nowUtc : patient.CreatedAt,
                UpdatedAtUtc = nowUtc
            };

            await _context.Patients.AddAsync(entity, ct);
            AddOrUpdateEmergencyContact(entity.Id, patient);

            if (patient.AppUserId.HasValue)
            {
                await UpsertPatientAccountAsync(entity.Id, patient.AppUserId.Value, nowUtc, ct);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Patient patient, CancellationToken ct = default)
        {
            var entity = await _context.Patients
                .FirstOrDefaultAsync(x => x.Id == patient.Id && x.DeletedAtUtc == null, ct);
            if (entity == null)
            {
                return;
            }

            entity.FullName = patient.FullName.Trim();
            entity.DateOfBirth = DateOnly.FromDateTime(patient.DateOfBirth);
            entity.Gender = patient.Gender.Trim();
            entity.Phone = patient.Phone.Trim();
            entity.AddressLine1 = patient.Address.Trim();
            entity.UpdatedAtUtc = DateTime.UtcNow;

            AddOrUpdateEmergencyContact(entity.Id, patient);

            if (patient.AppUserId.HasValue)
            {
                await UpsertPatientAccountAsync(entity.Id, patient.AppUserId.Value, DateTime.UtcNow, ct);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task MergeAsync(Patient sourcePatient, Patient targetPatient, CancellationToken ct = default)
        {
            await UpdateAsync(targetPatient, ct);

            var sourceEntity = await _context.Patients
                .Include(x => x.PatientAccount)
                .Include(x => x.EmergencyContacts)
                .FirstOrDefaultAsync(x => x.Id == sourcePatient.Id, ct);

            if (sourceEntity == null)
            {
                return;
            }

            sourceEntity.DeletedAtUtc ??= DateTime.UtcNow;
            sourceEntity.UpdatedAtUtc = DateTime.UtcNow;

            if (sourceEntity.PatientAccount != null)
            {
                _context.PatientAccounts.Remove(sourceEntity.PatientAccount);
            }

            if (sourceEntity.EmergencyContacts.Count > 0)
            {
                _context.PatientEmergencyContacts.RemoveRange(sourceEntity.EmergencyContacts);
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Patient patient, CancellationToken ct = default)
        {
            var entity = await _context.Patients.FirstOrDefaultAsync(x => x.Id == patient.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.DeletedAtUtc ??= DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        private IQueryable<PatientJoinRow> BuildPatientQuery()
        {
            return from patient in _context.Patients.AsNoTracking()
                   where patient.DeletedAtUtc == null
                   join patientAccount in _context.PatientAccounts.AsNoTracking()
                       on patient.Id equals patientAccount.PatientId into patientAccountGroup
                   from patientAccount in patientAccountGroup.DefaultIfEmpty()
                   join emergencyContact in _context.PatientEmergencyContacts.AsNoTracking()
                       on patient.Id equals emergencyContact.PatientId into emergencyContactGroup
                   from emergencyContact in emergencyContactGroup
                       .OrderBy(x => x.Id)
                       .Take(1)
                       .DefaultIfEmpty()
                   select new PatientJoinRow
                   {
                       Patient = patient,
                       AppUserId = patientAccount != null ? patientAccount.UserId : null,
                       EmergencyContact = emergencyContact
                   };
        }

        private static Patient MapPatient(PatientJoinRow row)
        {
            return new Patient
            {
                Id = row.Patient.Id,
                AppUserId = row.AppUserId,
                FullName = row.Patient.FullName,
                DateOfBirth = row.Patient.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                Gender = row.Patient.Gender,
                Phone = row.Patient.Phone ?? string.Empty,
                Address = string.Join(", ", new[]
                {
                    row.Patient.AddressLine1,
                    row.Patient.AddressLine2,
                    row.Patient.Ward,
                    row.Patient.District,
                    row.Patient.Province
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                EmergencyContactName = row.EmergencyContact?.FullName,
                EmergencyContactPhone = row.EmergencyContact?.Phone,
                EmergencyContactRelationship = row.EmergencyContact?.Relationship,
                CreatedAt = row.Patient.CreatedAtUtc
            };
        }

        private void AddOrUpdateEmergencyContact(Guid patientId, Patient patient)
        {
            var existingContacts = _context.PatientEmergencyContacts.Where(x => x.PatientId == patientId);
            _context.PatientEmergencyContacts.RemoveRange(existingContacts);

            if (string.IsNullOrWhiteSpace(patient.EmergencyContactName) ||
                string.IsNullOrWhiteSpace(patient.EmergencyContactPhone) ||
                string.IsNullOrWhiteSpace(patient.EmergencyContactRelationship))
            {
                return;
            }

            _context.PatientEmergencyContacts.Add(new HospitalPatientEmergencyContactEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                FullName = patient.EmergencyContactName.Trim(),
                Phone = patient.EmergencyContactPhone.Trim(),
                Relationship = patient.EmergencyContactRelationship.Trim(),
                Address = patient.Address.Trim()
            });
        }

        private async Task UpsertPatientAccountAsync(Guid patientId, Guid userId, DateTime activatedAtUtc, CancellationToken ct)
        {
            var existingAccount = await _context.PatientAccounts
                .FirstOrDefaultAsync(x => x.PatientId == patientId, ct);

            if (existingAccount == null)
            {
                await _context.PatientAccounts.AddAsync(new HospitalPatientAccountEntity
                {
                    PatientId = patientId,
                    UserId = userId,
                    ActivatedAtUtc = activatedAtUtc,
                    PortalStatus = "Active"
                }, ct);

                return;
            }

            existingAccount.UserId = userId;
            existingAccount.ActivatedAtUtc = activatedAtUtc;
            existingAccount.PortalStatus = "Active";
        }

        private sealed class PatientJoinRow
        {
            public HospitalPatientEntity Patient { get; init; } = null!;
            public Guid? AppUserId { get; init; }
            public HospitalPatientEmergencyContactEntity? EmergencyContact { get; init; }
        }
    }
}
