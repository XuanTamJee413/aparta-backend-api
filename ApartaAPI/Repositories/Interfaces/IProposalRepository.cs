using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IProposalRepository : IRepository<Proposal>
    {
        // Resident: Lấy tất cả Proposal của Resident này
        Task<IEnumerable<Proposal>> GetResidentProposalsAsync(string residentId);

        // Staff: Lấy tất cả Proposal chưa xử lý mà Staff này phụ trách
        IQueryable<Proposal> GetStaffAssignedProposalsAsync(string staffId);

        // Staff: Lấy Proposal chi tiết (cần Include Resident)
        Task<Proposal?> GetProposalDetailsAsync(string proposalId);
        IQueryable<Proposal> GetStaffProposalsQuery(string staffId);
    }
}
