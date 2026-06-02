using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.Repositories
{
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly HospitalDbContext _context;

        public PrescriptionRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Prescription>> GetAllAsync(CancellationToken ct = default)
        {
            var prescriptions = await BuildPrescriptionQuery().ToListAsync(ct);
            return prescriptions.Select(HospitalLegacyRepositoryMapper.MapPrescription).ToList();
        }

        public async Task<(IEnumerable<Prescription> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = BuildPrescriptionQuery();
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items.Select(HospitalLegacyRepositoryMapper.MapPrescription).ToList(), totalCount);
        }

        public async Task<Dictionary<DateTime, int>> GetCreatedCountByDayAsync(DateTime fromUtc, CancellationToken ct = default)
        {
            return await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.CreatedAtUtc >= fromUtc)
                .GroupBy(p => p.CreatedAtUtc.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count, ct);
        }

        public async Task<Prescription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var prescription = await BuildPrescriptionQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
            return prescription == null ? null : HospitalLegacyRepositoryMapper.MapPrescription(prescription);
        }

        public async Task<Prescription?> GetByMedicalRecordIdAsync(Guid medicalRecordId, CancellationToken ct = default)
        {
            var prescription = await BuildPrescriptionQuery()
                .FirstOrDefaultAsync(x => x.OrderHeader.EncounterId == medicalRecordId, ct);
            return prescription == null ? null : HospitalLegacyRepositoryMapper.MapPrescription(prescription);
        }

        public async Task AddAsync(Prescription prescription, CancellationToken ct = default)
        {
            var encounter = await _context.Encounters.FirstAsync(x => x.Id == prescription.MedicalRecordId, ct);
            var orderHeader = await _context.OrderHeaders
                .FirstOrDefaultAsync(x => x.EncounterId == encounter.Id && x.OrderCategory == "Prescription", ct);

            if (orderHeader == null)
            {
                orderHeader = new HospitalOrderHeaderEntity
                {
                    Id = Guid.NewGuid(),
                    EncounterId = encounter.Id,
                    OrderNumber = $"ORD-{prescription.Id.ToString("N")[..8].ToUpperInvariant()}",
                    OrderCategory = "Prescription",
                    OrderStatus = "Completed",
                    OrderedAtUtc = prescription.CreatedAt
                };

                await _context.OrderHeaders.AddAsync(orderHeader, ct);
            }

            await _context.Prescriptions.AddAsync(new HospitalPrescriptionEntity
            {
                Id = prescription.Id,
                OrderHeaderId = orderHeader.Id,
                PrescriptionNumber = $"RX-{prescription.Id.ToString("N")[..8].ToUpperInvariant()}",
                Status = "Issued",
                CreatedAtUtc = prescription.CreatedAt
            }, ct);

            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Prescription prescription, CancellationToken ct = default)
        {
            var entity = await _context.Prescriptions.FirstOrDefaultAsync(x => x.Id == prescription.Id, ct);
            if (entity == null)
            {
                return;
            }

            var items = _context.PrescriptionItems.Where(x => x.PrescriptionId == entity.Id);
            _context.PrescriptionItems.RemoveRange(items);
            _context.Prescriptions.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }

        public Task<bool> MedicalRecordExistsAsync(Guid medicalRecordId, CancellationToken ct = default)
            => _context.Encounters.AnyAsync(x => x.Id == medicalRecordId, ct);

        public Task<bool> PrescriptionExistsForMedicalRecordAsync(Guid medicalRecordId, CancellationToken ct = default)
            => _context.Prescriptions.AnyAsync(x => x.OrderHeader.EncounterId == medicalRecordId, ct);

        private IQueryable<HospitalPrescriptionEntity> BuildPrescriptionQuery()
        {
            return _context.Prescriptions
                .AsNoTracking()
                .Include(x => x.OrderHeader)
                .Include(x => x.PrescriptionItems)
                .ThenInclude(x => x.Medicine);
        }
    }
}
