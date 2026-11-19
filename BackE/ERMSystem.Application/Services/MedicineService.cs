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
    public class MedicineService : IMedicineService
    {
        private readonly IMedicineRepository _medicineRepository;

        public MedicineService(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
        }

        public async Task<PaginatedResult<MedicineDto>> GetAllMedicinesAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _medicineRepository.GetPagedAsync(request.PageNumber, request.PageSize, ct);
            return new PaginatedResult<MedicineDto>(items.Select(MapToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<MedicineDto?> GetMedicineByIdAsync(Guid id, CancellationToken ct = default)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id, ct);
            return medicine == null ? null : MapToDto(medicine);
        }

        public async Task<MedicineDto> CreateMedicineAsync(CreateMedicineDto dto, CancellationToken ct = default)
        {
            var medicine = Medicine.Create(dto.Name, dto.Description);

            await _medicineRepository.AddAsync(medicine, ct);
            return MapToDto(medicine);
        }

        public async Task UpdateMedicineAsync(Guid id, UpdateMedicineDto dto, CancellationToken ct = default)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id, ct);
            if (medicine == null)
                throw new KeyNotFoundException($"Medicine with ID {id} not found.");

            medicine.Update(dto.Name, dto.Description);

            await _medicineRepository.UpdateAsync(medicine, ct);
        }

        public async Task DeleteMedicineAsync(Guid id, CancellationToken ct = default)
        {
            var medicine = await _medicineRepository.GetByIdAsync(id, ct);
            if (medicine == null)
                throw new KeyNotFoundException($"Medicine with ID {id} not found.");

            await _medicineRepository.DeleteAsync(medicine, ct);
        }

        private static MedicineDto MapToDto(Medicine m) => new MedicineDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description
        };
    }
}
