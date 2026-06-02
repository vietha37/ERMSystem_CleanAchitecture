using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class PrescriptionItemRepository : IPrescriptionItemRepository
    {
        private readonly HospitalDbContext _context;

        public PrescriptionItemRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<PrescriptionItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _context.PrescriptionItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return item == null ? null : HospitalLegacyRepositoryMapper.MapPrescriptionItem(item);
        }

        public async Task AddAsync(PrescriptionItem item, CancellationToken ct = default)
        {
            var durationDays = TryParseDurationDays(item.Duration);

            await _context.PrescriptionItems.AddAsync(new HospitalPrescriptionItemEntity
            {
                Id = item.Id,
                PrescriptionId = item.PrescriptionId,
                MedicineId = item.MedicineId,
                DoseInstruction = item.Dosage,
                Frequency = durationDays == null ? item.Duration : null,
                DurationDays = durationDays,
                Quantity = Math.Max(1, durationDays ?? 1)
            }, ct);

            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(PrescriptionItem item, CancellationToken ct = default)
        {
            var entity = await _context.PrescriptionItems.FirstOrDefaultAsync(x => x.Id == item.Id, ct);
            if (entity == null)
            {
                return;
            }

            _context.PrescriptionItems.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }

        public Task<bool> MedicineExistsAsync(Guid medicineId, CancellationToken ct = default)
            => _context.Medicines.AnyAsync(x => x.Id == medicineId && x.IsActive, ct);

        public Task<bool> PrescriptionExistsAsync(Guid prescriptionId, CancellationToken ct = default)
            => _context.Prescriptions.AnyAsync(x => x.Id == prescriptionId, ct);

        private static int? TryParseDurationDays(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return null;
            }

            var digits = new string(duration.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : null;
        }
    }
}
