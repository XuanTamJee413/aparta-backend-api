using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class StaffAssignmentRepository : Repository<StaffBuildingAssignment>, IStaffAssignmentRepository
    {
        public StaffAssignmentRepository(ApartaDbContext context) : base(context)
        {
        }

        public async Task<StaffBuildingAssignment?> GetActiveAssignmentAsync(string userId, string buildingId)
        {
            return await _dbSet.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.BuildingId == buildingId &&
                x.IsActive == true);
        }

        public IQueryable<StaffBuildingAssignment> GetAssignmentsQuery()
        {
            return _dbSet
                .Include(x => x.User)
                .Include(x => x.Building)
                .AsQueryable();
        }
    }
}