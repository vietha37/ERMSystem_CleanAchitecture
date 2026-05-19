using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Helpers;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly IDashboardQueryCache _dashboardQueryCache;

        public PrescriptionService(
            IPrescriptionRepository prescriptionRepository,
            IDashboardQueryCache dashboardQueryCache)
        {
            _prescriptionRepository = prescriptionRepository;
            _dashboardQueryCache = dashboardQueryCache;
        }

        public async Task<PaginatedResult<PrescriptionDto>> GetAllPrescriptionsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _prescriptionRepository.GetPagedAsync(request.PageNumber, request.PageSize, ct);
            return new PaginatedResult<PrescriptionDto>(items.Select(PrescriptionMapper.ToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id, CancellationToken ct = default)
        {
            var prescription = await _prescriptionRepository.GetByIdAsync(id, ct);
            return prescription == null ? null : PrescriptionMapper.ToDto(prescription);
        }

        public async Task<PrescriptionDto?> GetPrescriptionByMedicalRecordIdAsync(Guid medicalRecordId, CancellationToken ct = default)
        {
            var prescription = await _prescriptionRepository.GetByMedicalRecordIdAsync(medicalRecordId, ct);
            return prescription == null ? null : PrescriptionMapper.ToDto(prescription);
        }

        public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto dto, CancellationToken ct = default)
        {
            var medicalRecordExists = await _prescriptionRepository.MedicalRecordExistsAsync(dto.MedicalRecordId, ct);
            if (!medicalRecordExists)
                throw new KeyNotFoundException($"MedicalRecord with ID {dto.MedicalRecordId} not found.");

            var prescriptionAlreadyExists = await _prescriptionRepository
                .PrescriptionExistsForMedicalRecordAsync(dto.MedicalRecordId, ct);
            if (prescriptionAlreadyExists)
                throw new InvalidOperationException(
                    $"A Prescription already exists for MedicalRecord {dto.MedicalRecordId}.");

            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = dto.MedicalRecordId,
                CreatedAt = DateTime.UtcNow
            };

            await _prescriptionRepository.AddAsync(prescription, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
            return PrescriptionMapper.ToDto(prescription);
        }

        public async Task DeletePrescriptionAsync(Guid id, CancellationToken ct = default)
        {
            var prescription = await _prescriptionRepository.GetByIdAsync(id, ct);
            if (prescription == null)
                throw new KeyNotFoundException($"Prescription with ID {id} not found.");

            await _prescriptionRepository.DeleteAsync(prescription, ct);
            await _dashboardQueryCache.InvalidateAsync(ct);
        }
    }
}
