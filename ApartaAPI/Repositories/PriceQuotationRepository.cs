using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class PriceQuotationRepository : Repository<PriceQuotation>, IPriceQuotationRepository
    {
        public PriceQuotationRepository(ApartaDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PriceQuotation>> GetAllWithBuildingAsync()
        {
            return await _dbSet
                .Include(pq => pq.Building) 
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<PriceQuotation>> GetByBuildingIdWithBuildingAsync(string buildingId)
        {
            return await _dbSet
                .Where(pq => pq.BuildingId == buildingId) 
                .Include(pq => pq.Building) 
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PriceQuotation?> GetByIdWithBuildingAsync(string priceQuotationId)
        {
            return await _dbSet
                .Where(pq => pq.PriceQuotationId == priceQuotationId)
                .Include(pq => pq.Building)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
