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
            // Lấy tất cả Proposal được gán cho Staff này hoặc chưa được gán (Status = Pending)
            return await _dbSet
                .Where(p => p.OperationStaffId == staffId || p.Status == "Pending") // Thêm logic phân phối nếu cần
                .Include(p => p.Resident)
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
    }
}
