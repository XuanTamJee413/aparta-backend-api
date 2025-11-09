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
        public IQueryable<VisitLog> GetStaffViewLogsQuery()
        {
            return _dbSet
                .Include(a => a.Apartment)
                .Include(v => v.Visitor)
                .Where(v => v.Status != "Deleted")
                .AsQueryable();
        }
    }
}
