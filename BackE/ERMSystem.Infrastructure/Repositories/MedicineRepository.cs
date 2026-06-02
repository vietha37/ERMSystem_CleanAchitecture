using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly HospitalDbContext _context;

        public MedicineRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Medicine>> GetAllAsync(CancellationToken ct = default)
        {
            var medicines = await _context.Medicines.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
            return medicines.Select(HospitalLegacyRepositoryMapper.MapMedicine).ToList();
        }

        public async Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Medicines.AsNoTracking().Where(x => x.IsActive);
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.Select(HospitalLegacyRepositoryMapper.MapMedicine).ToList(), totalCount);
        }

        public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var medicine = await _context.Medicines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return medicine == null ? null : HospitalLegacyRepositoryMapper.MapMedicine(medicine);
        }

        public async Task AddAsync(Medicine medicine, CancellationToken ct = default)
        {
            await _context.Medicines.AddAsync(new HospitalMedicineEntity
            {
                Id = medicine.Id,
                DrugCode = $"MED-{medicine.Id.ToString("N")[..8].ToUpperInvariant()}",
                Name = medicine.Name.Trim(),
                GenericName = medicine.Description.Trim(),
                IsActive = true
            }, ct);

            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Medicine medicine, CancellationToken ct = default)
        {
            var entity = await _context.Medicines.FirstOrDefaultAsync(x => x.Id == medicine.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.Name = medicine.Name.Trim();
            entity.GenericName = medicine.Description.Trim();
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Medicine medicine, CancellationToken ct = default)
        {
            var entity = await _context.Medicines.FirstOrDefaultAsync(x => x.Id == medicine.Id, ct);
            if (entity == null)
            {
                return;
            }

            entity.IsActive = false;
            await _context.SaveChangesAsync(ct);
        }
    }
}
