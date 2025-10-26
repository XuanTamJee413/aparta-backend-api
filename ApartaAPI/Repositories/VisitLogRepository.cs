using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class VisitLogRepository : Repository<VisitLog>, IVisitLogRepository
    {
        public VisitLogRepository(ApartaDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<VisitLog>> GetStaffViewLogsAsync()
        {
            return await _dbSet
                .Include(a => a.Apartment)
                .Include(v => v.Visitor)
                .Where(v => v.Status != "Deleted")
                .OrderByDescending(vl => vl.CheckinTime)
                .ToListAsync();
        }
    }
}
