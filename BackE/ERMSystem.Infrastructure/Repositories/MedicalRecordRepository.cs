using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class MedicalRecordRepository : IMedicalRecordRepository
    {
        private readonly HospitalDbContext _context;

        public MedicalRecordRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<MedicalRecord>> GetAllAsync(CancellationToken ct = default)
        {
            var encounters = await BuildEncounterQuery().ToListAsync(ct);
            return encounters.Select(MapMedicalRecord).ToList();
        }

        public async Task<(IEnumerable<MedicalRecord> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = BuildEncounterQuery();
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.Select(MapMedicalRecord).ToList(), totalCount);
        }

        public async Task<Dictionary<string, int>> GetTopDiagnosesAsync(int count, CancellationToken ct = default)
        {
            return await _context.Diagnoses
                .AsNoTracking()
                .Where(x => x.IsPrimary && x.DiagnosisName != "")
                .GroupBy(x => x.DiagnosisName)
                .Select(g => new { Diagnosis = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToDictionaryAsync(x => x.Diagnosis, x => x.Count, ct);
        }

        public async Task<MedicalRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var encounter = await BuildEncounterQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
            return encounter == null ? null : MapMedicalRecord(encounter);
        }

        public async Task<MedicalRecord?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var encounter = await BuildEncounterQuery().FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return encounter == null ? null : MapMedicalRecord(encounter);
        }

        public async Task AddAsync(MedicalRecord record, CancellationToken ct = default)
        {
            var appointment = await _context.Appointments.FirstAsync(x => x.Id == record.AppointmentId, ct);
            var nowUtc = DateTime.UtcNow;
            var encounter = new HospitalEncounterEntity
            {
                Id = record.Id,
                EncounterNumber = $"ENC-{record.Id.ToString("N")[..8].ToUpperInvariant()}",
                PatientId = appointment.PatientId,
                AppointmentId = appointment.Id,
                DoctorProfileId = appointment.DoctorProfileId,
                ClinicId = appointment.ClinicId,
                EncounterType = "Outpatient",
                EncounterStatus = "Finalized",
                StartedAtUtc = record.CreatedAt,
                EndedAtUtc = record.CreatedAt,
                Summary = record.Notes,
                CreatedAtUtc = record.CreatedAt,
                UpdatedAtUtc = nowUtc
            };

            var note = new HospitalClinicalNoteEntity
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                NoteType = "General",
                Subjective = record.Symptoms,
                Assessment = record.Diagnosis,
                CarePlan = record.Notes,
                AuthoredAtUtc = record.CreatedAt
            };

            var diagnosis = new HospitalDiagnosisEntity
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                DiagnosisType = "Primary",
                DiagnosisName = record.Diagnosis,
                IsPrimary = true,
                NotedAtUtc = record.CreatedAt
            };

            await _context.Encounters.AddAsync(encounter, ct);
            await _context.ClinicalNotes.AddAsync(note, ct);
            await _context.Diagnoses.AddAsync(diagnosis, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(MedicalRecord record, CancellationToken ct = default)
        {
            var encounter = await _context.Encounters.FirstOrDefaultAsync(x => x.Id == record.Id, ct);
            if (encounter == null)
            {
                return;
            }

            encounter.Summary = record.Notes;
            encounter.UpdatedAtUtc = DateTime.UtcNow;

            var note = await _context.ClinicalNotes.FirstOrDefaultAsync(x => x.EncounterId == record.Id, ct);
            if (note == null)
            {
                await _context.ClinicalNotes.AddAsync(new HospitalClinicalNoteEntity
                {
                    Id = Guid.NewGuid(),
                    EncounterId = record.Id,
                    NoteType = "General",
                    Subjective = record.Symptoms,
                    Assessment = record.Diagnosis,
                    CarePlan = record.Notes,
                    AuthoredAtUtc = DateTime.UtcNow
                }, ct);
            }
            else
            {
                note.Subjective = record.Symptoms;
                note.Assessment = record.Diagnosis;
                note.CarePlan = record.Notes;
            }

            var diagnosis = await _context.Diagnoses
                .FirstOrDefaultAsync(x => x.EncounterId == record.Id && x.IsPrimary, ct);
            if (diagnosis == null)
            {
                await _context.Diagnoses.AddAsync(new HospitalDiagnosisEntity
                {
                    Id = Guid.NewGuid(),
                    EncounterId = record.Id,
                    DiagnosisType = "Primary",
                    DiagnosisName = record.Diagnosis,
                    IsPrimary = true,
                    NotedAtUtc = DateTime.UtcNow
                }, ct);
            }
            else
            {
                diagnosis.DiagnosisName = record.Diagnosis;
            }

            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(MedicalRecord record, CancellationToken ct = default)
        {
            var encounter = await _context.Encounters.FirstOrDefaultAsync(x => x.Id == record.Id, ct);
            if (encounter == null)
            {
                return;
            }

            var diagnoses = _context.Diagnoses.Where(x => x.EncounterId == record.Id);
            var notes = _context.ClinicalNotes.Where(x => x.EncounterId == record.Id);
            _context.Diagnoses.RemoveRange(diagnoses);
            _context.ClinicalNotes.RemoveRange(notes);
            _context.Encounters.Remove(encounter);
            await _context.SaveChangesAsync(ct);
        }

        public Task<bool> AppointmentExistsAsync(Guid appointmentId, CancellationToken ct = default)
            => _context.Appointments.AnyAsync(x => x.Id == appointmentId, ct);

        public Task<bool> MedicalRecordExistsForAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
            => _context.Encounters.AnyAsync(x => x.AppointmentId == appointmentId, ct);

        private IQueryable<HospitalEncounterEntity> BuildEncounterQuery()
        {
            return _context.Encounters
                .AsNoTracking()
                .Include(x => x.Appointment)
                .Include(x => x.ClinicalNotes)
                .Include(x => x.Diagnoses);
        }

        private static MedicalRecord MapMedicalRecord(HospitalEncounterEntity encounter)
        {
            var note = encounter.ClinicalNotes.OrderByDescending(x => x.AuthoredAtUtc).FirstOrDefault();
            var diagnosis = encounter.Diagnoses
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.NotedAtUtc)
                .FirstOrDefault();

            return HospitalLegacyRepositoryMapper.MapMedicalRecord(encounter, note, diagnosis);
        }
    }
}
