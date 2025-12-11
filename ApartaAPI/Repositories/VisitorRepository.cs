using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class VisitorRepository : Repository<Visitor>, IVisitorRepository
    {
        public VisitorRepository(ApartaDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Visitor>> GetRecentVisitorsByApartmentAsync(string apartmentId)
        {
            return await _context.VisitLogs
                .AsNoTracking()
                .Where(vl => vl.ApartmentId == apartmentId)
                .Select(vl => vl.Visitor)
                .GroupBy(v => v.VisitorId) 
                .Select(g => g.First())
                .ToListAsync();
        }
    }
}
