using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Proposals;

namespace ApartaAPI.Services.Interfaces
{
    public interface IProposalService
    {
        // Resident: Tạo Proposal mới
        Task<ProposalDto> CreateProposalAsync(string residentId, ProposalCreateDto createDto);

        // Resident: Lấy lịch sử
        Task<IEnumerable<ProposalDto>> GetProposalsByResidentAsync(string residentId);

        // Staff: Lấy danh sách Proposal cần xử lý
        Task<ApiResponse<PagedList<ProposalDto>>> GetProposalsForStaffAsync(string staffId, ProposalQueryParams query);

        // Staff/Resident: Xem chi tiết
        Task<ProposalDto?> GetProposalDetailAsync(string proposalId, string currentUserId);

        // Staff: Trả lời Proposal
        Task<ProposalDto?> ReplyProposalAsync(string proposalId, string staffId, ProposalReplyDto replyDto);
    }
}
