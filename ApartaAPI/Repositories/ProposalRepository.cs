using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class ProposalRepository : Repository<Proposal>, IProposalRepository
    {
        public ProposalRepository(ApartaDbContext context) : base(context) { }

        public async Task<IEnumerable<Proposal>> GetResidentProposalsAsync(string residentId)
        {
            return await _dbSet
                .Where(p => p.ResidentId == residentId)
                .Include(p => p.OperationStaff)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Proposal>> GetStaffAssignedProposalsAsync(string staffId)
        {
            // 1. Lấy danh sách các tòa nhà mà staff đang phụ trách (IsActive = true)
            var buildingIds = await _context.Set<StaffBuildingAssignment>()
                .Where(sba => sba.UserId == staffId && sba.IsActive)
                .Select(sba => sba.BuildingId)
                .ToListAsync();
            return await _dbSet
                .Include(p => p.Resident)
                .ThenInclude(u => u.Apartment) // Load Apartment của Resident để check BuildingId
                .Where(p =>
                    p.OperationStaffId == staffId ||
                    (
                        p.Status == "Pending" &&
                        p.Resident.ApartmentId != null &&
                        buildingIds.Contains(p.Resident.Apartment.BuildingId)
                    )
                )
                .Include(p => p.OperationStaff) 
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Proposal?> GetProposalDetailsAsync(string proposalId)
        {
            return await _dbSet
                .Include(p => p.Resident)
                .Include(p => p.OperationStaff)
                .FirstOrDefaultAsync(p => p.ProposalId == proposalId);
        }
        public IQueryable<Proposal> GetStaffProposalsQuery(string staffId)
        {
            // LẤY PROPOSAL ĐƯỢC GÁN CHO STAFF HIỆN TẠI: OperationStaffId == staffId
            return _dbSet
                .Where(p => p.OperationStaffId == staffId)
                .Include(p => p.Resident)
                .Include(p => p.OperationStaff)
                .AsQueryable();
        }
    }
}
