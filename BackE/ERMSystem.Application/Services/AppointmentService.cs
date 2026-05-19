using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private static readonly HashSet<string> ValidStatuses =
            new HashSet<string>(StringComparer.Ordinal) { "Pending", "Completed", "Cancelled" };

        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDashboardQueryCache _dashboardQueryCache;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IDashboardQueryCache dashboardQueryCache)
        {
            _appointmentRepository = appointmentRepository;
            _dashboardQueryCache = dashboardQueryCache;
        }

        public async Task<PaginatedResult<AppointmentDto>> GetAllAppointmentsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _appointmentRepository.GetPagedAsync(request.PageNumber, request.PageSize, ct);
            return new PaginatedResult<AppointmentDto>(items.Select(MapToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(Guid id, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id, ct);
            return appointment == null ? null : MapToDto(appointment);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto, CancellationToken ct = default)
        {
            if (!ValidStatuses.Contains(dto.Status))
                throw new ArgumentException(
                    $"Invalid status '{dto.Status}'. Must be Pending, Completed, or Cancelled.");

            var patientExists = await _appointmentRepository.PatientExistsAsync(dto.PatientId, ct);
            if (!patientExists)
                throw new KeyNotFoundException($"Patient with ID {dto.PatientId} not found.");

            var doctorExists = await _appointmentRepository.DoctorExistsAsync(dto.DoctorId, ct);
            if (!doctorExists)
                throw new KeyNotFoundException($"Doctor with ID {dto.DoctorId} not found.");

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDate = dto.AppointmentDate,
                Status = dto.Status
            };

            await _appointmentRepository.AddAsync(appointment, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
            return MapToDto(appointment);
        }

        public async Task UpdateAppointmentAsync(Guid id, UpdateAppointmentDto dto, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id, ct);
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {id} not found.");

            if (!ValidStatuses.Contains(dto.Status))
                throw new ArgumentException(
                    $"Invalid status '{dto.Status}'. Must be Pending, Completed, or Cancelled.");

            var patientExists = await _appointmentRepository.PatientExistsAsync(dto.PatientId, ct);
            if (!patientExists)
                throw new KeyNotFoundException($"Patient with ID {dto.PatientId} not found.");

            var doctorExists = await _appointmentRepository.DoctorExistsAsync(dto.DoctorId, ct);
            if (!doctorExists)
                throw new KeyNotFoundException($"Doctor with ID {dto.DoctorId} not found.");

            appointment.PatientId = dto.PatientId;
            appointment.DoctorId = dto.DoctorId;
            appointment.AppointmentDate = dto.AppointmentDate;
            appointment.Status = dto.Status;

            await _appointmentRepository.UpdateAsync(appointment, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
        }

        public async Task DeleteAppointmentAsync(Guid id, CancellationToken ct = default)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id, ct);
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {id} not found.");

            await _appointmentRepository.DeleteAsync(appointment, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
        }

        private static AppointmentDto MapToDto(Appointment a) => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            AppointmentDate = a.AppointmentDate,
            Status = a.Status
        };
    }
}
