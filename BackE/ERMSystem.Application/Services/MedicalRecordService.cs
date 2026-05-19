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
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly IMedicalRecordRepository _medicalRecordRepository;
        private readonly IDashboardQueryCache _dashboardQueryCache;

        public MedicalRecordService(
            IMedicalRecordRepository medicalRecordRepository,
            IDashboardQueryCache dashboardQueryCache)
        {
            _medicalRecordRepository = medicalRecordRepository;
            _dashboardQueryCache = dashboardQueryCache;
        }

        public async Task<PaginatedResult<MedicalRecordDto>> GetAllMedicalRecordsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _medicalRecordRepository.GetPagedAsync(request.PageNumber, request.PageSize, ct);
            return new PaginatedResult<MedicalRecordDto>(items.Select(MapToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<MedicalRecordDto?> GetMedicalRecordByIdAsync(Guid id, CancellationToken ct = default)
        {
            var record = await _medicalRecordRepository.GetByIdAsync(id, ct);
            return record == null ? null : MapToDto(record);
        }

        public async Task<MedicalRecordDto?> GetMedicalRecordByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var record = await _medicalRecordRepository.GetByAppointmentIdAsync(appointmentId, ct);
            return record == null ? null : MapToDto(record);
        }

        public async Task<MedicalRecordDto> CreateMedicalRecordAsync(CreateMedicalRecordDto dto, CancellationToken ct = default)
        {
            var appointmentExists = await _medicalRecordRepository.AppointmentExistsAsync(dto.AppointmentId, ct);
            if (!appointmentExists)
                throw new KeyNotFoundException($"Appointment with ID {dto.AppointmentId} not found.");

            var recordAlreadyExists = await _medicalRecordRepository
                .MedicalRecordExistsForAppointmentAsync(dto.AppointmentId, ct);
            if (recordAlreadyExists)
                throw new InvalidOperationException(
                    $"A MedicalRecord already exists for Appointment {dto.AppointmentId}.");

            var record = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                AppointmentId = dto.AppointmentId,
                CreatedAt = DateTime.UtcNow,
                Symptoms = dto.Symptoms,
                Diagnosis = dto.Diagnosis,
                Notes = dto.Notes
            };

            await _medicalRecordRepository.AddAsync(record, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
            return MapToDto(record);
        }

        public async Task UpdateMedicalRecordAsync(Guid id, UpdateMedicalRecordDto dto, CancellationToken ct = default)
        {
            var record = await _medicalRecordRepository.GetByIdAsync(id, ct);
            if (record == null)
                throw new KeyNotFoundException($"MedicalRecord with ID {id} not found.");

            record.Symptoms = dto.Symptoms;
            record.Diagnosis = dto.Diagnosis;
            record.Notes = dto.Notes;

            await _medicalRecordRepository.UpdateAsync(record, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
        }

        public async Task DeleteMedicalRecordAsync(Guid id, CancellationToken ct = default)
        {
            var record = await _medicalRecordRepository.GetByIdAsync(id, ct);
            if (record == null)
                throw new KeyNotFoundException($"MedicalRecord with ID {id} not found.");

            await _medicalRecordRepository.DeleteAsync(record, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
        }

        private static MedicalRecordDto MapToDto(MedicalRecord r) => new MedicalRecordDto
        {
            Id = r.Id,
            AppointmentId = r.AppointmentId,
            CreatedAt = r.CreatedAt,
            Symptoms = r.Symptoms,
            Diagnosis = r.Diagnosis,
            Notes = r.Notes
        };
    }
}
