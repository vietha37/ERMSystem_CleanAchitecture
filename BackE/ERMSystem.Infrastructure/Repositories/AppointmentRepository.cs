using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.Data;

namespace ERMSystem.Infrastructure.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _context;

        public AppointmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment>> GetAllAsync(CancellationToken ct = default)
            => await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ToListAsync(ct);

        public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var totalCount = await _context.Appointments.CountAsync(ct);
            var items = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return (items, totalCount);
        }

        public async Task<int> GetAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today)
                .CountAsync(ct);
        }

        public async Task<int> GetCompletedAppointmentsCountAsync(CancellationToken ct = default)
            => await _context.Appointments
                .Where(a => a.Status == "Completed")
                .CountAsync(ct);

        public async Task<int> GetPendingAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && a.Status == "Pending")
                .CountAsync(ct);
        }

        public async Task<int> GetCompletedAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && a.Status == "Completed")
                .CountAsync(ct);
        }

        public async Task<int> GetCancelledAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && a.Status == "Cancelled")
                .CountAsync(ct);
        }

        public async Task<int> GetRevisitAppointmentsTodayCountAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today)
                .Where(a => _context.Appointments.Any(previous =>
                    previous.PatientId == a.PatientId &&
                    previous.AppointmentDate < today))
                .CountAsync(ct);
        }

        public async Task<Dictionary<DateTime, int>> GetScheduledCountByDayAsync(
            DateTime fromUtc,
            CancellationToken ct = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentDate >= fromUtc)
                .GroupBy(a => a.AppointmentDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        }

        public async Task<List<Appointment>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
            => await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= fromUtc && a.AppointmentDate <= toUtc)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync(ct);

        public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task AddAsync(Appointment appointment, CancellationToken ct = default)
        {
            await _context.Appointments.AddAsync(appointment, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Appointment appointment, CancellationToken ct = default)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> PatientExistsAsync(Guid patientId, CancellationToken ct = default)
            => await _context.Patients.AnyAsync(p => p.Id == patientId, ct);

        public async Task<bool> DoctorExistsAsync(Guid doctorId, CancellationToken ct = default)
            => await _context.Doctors.AnyAsync(d => d.Id == doctorId, ct);
    }
}
