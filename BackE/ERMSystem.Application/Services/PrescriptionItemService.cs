using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Helpers;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services
{
    public class PrescriptionItemService : IPrescriptionItemService
    {
        private readonly IPrescriptionItemRepository _itemRepository;
        private readonly IPrescriptionRepository _prescriptionRepository;

        public PrescriptionItemService(
            IPrescriptionItemRepository itemRepository,
            IPrescriptionRepository prescriptionRepository)
        {
            _itemRepository = itemRepository;
            _prescriptionRepository = prescriptionRepository;
        }

        public async Task<PrescriptionDto> AddItemToPrescriptionAsync(
            Guid prescriptionId,
            AddPrescriptionItemDto dto,
            CancellationToken ct = default)
        {
            var prescriptionExists = await _itemRepository.PrescriptionExistsAsync(prescriptionId, ct);
            if (!prescriptionExists)
                throw new KeyNotFoundException($"Prescription with ID {prescriptionId} not found.");

            var medicineExists = await _itemRepository.MedicineExistsAsync(dto.MedicineId, ct);
            if (!medicineExists)
                throw new KeyNotFoundException($"Medicine with ID {dto.MedicineId} not found.");

            var item = PrescriptionItem.Create(prescriptionId, dto.MedicineId, dto.Dosage, dto.Duration);

            await _itemRepository.AddAsync(item, ct);

            // Reload the full prescription with updated items
            var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId, ct);
            return PrescriptionMapper.ToDto(prescription!);
        }

        public async Task<PrescriptionDto> RemoveItemFromPrescriptionAsync(
            Guid prescriptionId,
            Guid itemId,
            CancellationToken ct = default)
        {
            var item = await _itemRepository.GetByIdAsync(itemId, ct);
            if (item == null || item.PrescriptionId != prescriptionId)
                throw new KeyNotFoundException(
                    $"PrescriptionItem {itemId} not found on Prescription {prescriptionId}.");

            await _itemRepository.DeleteAsync(item, ct);

            // Reload the full prescription with updated items
            var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId, ct);
            return PrescriptionMapper.ToDto(prescription!);
        }
    }
}
