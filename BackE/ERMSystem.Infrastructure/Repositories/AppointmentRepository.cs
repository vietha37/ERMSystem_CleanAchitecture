using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HospitalDbContext _context;

        public AppointmentRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment>> GetAllAsync(CancellationToken ct = default)
        {
            var appointments = await BuildAppointmentQuery().ToListAsync(ct);
            return appointments.Select(HospitalLegacyRepositoryMapper.MapAppointment).ToList();
        }

        public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = BuildAppointmentQuery();
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.AppointmentStartUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.Select(HospitalLegacyRepositoryMapper.MapAppointment).ToList(), totalCount);
        }

        public Task<int> GetAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return _context.Appointments.CountAsync(a => a.AppointmentStartUtc.Date == today, ct);
        }

        public Task<int> GetCompletedAppointmentsCountAsync(CancellationToken ct = default)
            => _context.Appointments.CountAsync(a => a.Status == "Completed", ct);

        public Task<int> GetPendingAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return _context.Appointments.CountAsync(
                a => a.AppointmentStartUtc.Date == today && a.Status != "Completed" && a.Status != "Cancelled",
                ct);
        }

        public Task<int> GetCompletedAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return _context.Appointments.CountAsync(a => a.AppointmentStartUtc.Date == today && a.Status == "Completed", ct);
        }

        public Task<int> GetCancelledAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return _context.Appointments.CountAsync(a => a.AppointmentStartUtc.Date == today && a.Status == "Cancelled", ct);
        }

        public Task<int> GetRevisitAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return _context.Appointments.CountAsync(a =>
                a.AppointmentStartUtc.Date == today &&
                _context.Appointments.Any(previous =>
                    previous.PatientId == a.PatientId &&
                    previous.AppointmentStartUtc < today),
                ct);
        }

        public async Task<Dictionary<DateTime, int>> GetScheduledCountByDayAsync(DateTime fromUtc, CancellationToken ct = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentStartUtc >= fromUtc)
                .GroupBy(a => a.AppointmentStartUtc.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        }

        public async Task<List<Appointment>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            var appointments = await BuildAppointmentQuery()
                .Where(a => a.AppointmentStartUtc >= fromUtc && a.AppointmentStartUtc <= toUtc)
                .OrderBy(a => a.AppointmentStartUtc)
                .ToListAsync(ct);

            return appointments.Select(HospitalLegacyRepositoryMapper.MapAppointment).ToList();
        }

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var appointment = await BuildAppointmentQuery().FirstOrDefaultAsync(a => a.Id == id, ct);
            return appointment == null ? null : HospitalLegacyRepositoryMapper.MapAppointment(appointment);
        }

        public async Task AddAsync(Appointment appointment, CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;
            var doctorProfile = await _context.DoctorProfiles
                .Include(x => x.DoctorSchedules.Where(s => s.IsActive))
                .FirstAsync(x => x.Id == appointment.DoctorId, ct);

            var clinicId = doctorProfile.DoctorSchedules
                .OrderBy(x => x.ValidFrom)
                .Select(x => (Guid?)x.ClinicId)
                .FirstOrDefault()
                ?? await _context.Clinics
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => x.Id)
                    .FirstAsync(ct);

            var entity = new HospitalAppointmentEntity
            {
                Id = appointment.Id,
                AppointmentNumber = $"APT-{appointment.Id.ToString("N")[..8].ToUpperInvariant()}",
                PatientId = appointment.PatientId,
                DoctorProfileId = appointment.DoctorId,
                ClinicId = clinicId,
                AppointmentType = "General",
                BookingChannel = "Internal",
                Status = MapStatusToHospital(appointment.Status),
                AppointmentStartUtc = appointment.AppointmentDate,
                AppointmentEndUtc = appointment.AppointmentDate.AddMinutes(30),
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            await _context.Appointments.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
        {
            var entity = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == appointment.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.PatientId = appointment.PatientId;
            entity.DoctorProfileId = appointment.DoctorId;
            entity.Status = MapStatusToHospital(appointment.Status);
            entity.AppointmentStartUtc = appointment.AppointmentDate;
            entity.AppointmentEndUtc = appointment.AppointmentDate.AddMinutes(30);
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Appointment appointment, CancellationToken ct = default)
        {
            var entity = await _context.Appointments.FirstOrDefaultAsync(x => x.Id == appointment.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.Status = "Cancelled";
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }

        public Task<bool> PatientExistsAsync(Guid patientId, CancellationToken ct = default)
            => _context.Patients.AnyAsync(p => p.Id == patientId && p.DeletedAtUtc == null, ct);

        public Task<bool> DoctorExistsAsync(Guid doctorId, CancellationToken ct = default)
            => _context.DoctorProfiles.AnyAsync(d => d.Id == doctorId, ct);

        private IQueryable<HospitalAppointmentEntity> BuildAppointmentQuery()
        {
            return _context.Appointments
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.StaffProfile);
        }

        private static string MapStatusToHospital(string status)
        {
            return status switch
            {
                "Completed" => "Completed",
                "Cancelled" => "Cancelled",
                _ => "Scheduled"
            };
        }
    }
}
