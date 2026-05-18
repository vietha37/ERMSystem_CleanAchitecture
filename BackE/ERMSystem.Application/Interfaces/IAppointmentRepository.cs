using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> GetAllAsync(CancellationToken ct = default);
        Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task<int> GetAppointmentsTodayCountAsync(CancellationToken ct = default);
        Task<int> GetCompletedAppointmentsCountAsync(CancellationToken ct = default);
        Task<int> GetPendingAppointmentsTodayCountAsync(CancellationToken ct = default);
        Task<int> GetCompletedAppointmentsTodayCountAsync(CancellationToken ct = default);
        Task<int> GetCancelledAppointmentsTodayCountAsync(CancellationToken ct = default);
        Task<int> GetRevisitAppointmentsTodayCountAsync(CancellationToken ct = default);
        Task<Dictionary<DateTime, int>> GetScheduledCountByDayAsync(DateTime fromUtc, CancellationToken ct = default);
        Task<List<Appointment>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
        Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task AddAsync(Appointment appointment, CancellationToken ct = default);
        Task UpdateAsync(Appointment appointment, CancellationToken ct = default);
        Task DeleteAsync(Appointment appointment, CancellationToken ct = default);
        Task<bool> PatientExistsAsync(Guid patientId, CancellationToken ct = default);
        Task<bool> DoctorExistsAsync(Guid doctorId, CancellationToken ct = default);
    }
}
