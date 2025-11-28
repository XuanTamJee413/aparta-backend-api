using ApartaAPI.Data;
using ApartaAPI.DTOs.Proposals;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class ProposalService : IProposalService
    {
        private readonly IProposalRepository _proposalRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<StaffBuildingAssignment> _sbaRepo;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;

        public ProposalService(
            IProposalRepository proposalRepo,
            IRepository<User> userRepo,
            IRepository<StaffBuildingAssignment> sbaRepo,
            IMapper mapper,
            ApartaDbContext context)
        {
            _proposalRepo = proposalRepo;
            _userRepo = userRepo;
            _sbaRepo = sbaRepo;
            _mapper = mapper;
            _context = context;
        }

        // Logic tìm Staff phụ trách tòa nhà của Resident
        private async Task<string?> FindResponsibleStaffId(string residentId)
        {
            var resident = await _context.Users.Include(u => u.Apartment).FirstOrDefaultAsync(u => u.UserId == residentId);
            if (resident?.Apartment?.BuildingId == null) return null;

            var buildingId = resident.Apartment.BuildingId;

            // Tìm Staff được gán (logic tương tự Chat Service)
            var assignment = await _sbaRepo.FirstOrDefaultAsync(sba => sba.BuildingId == buildingId && sba.IsActive);

            return assignment?.UserId;
        }


        // ============================
        // CREATE PROPOSAL (RESIDENT)
        // ============================
        public async Task<ProposalDto> CreateProposalAsync(string residentId, ProposalCreateDto createDto)
        {
            var staffId = await FindResponsibleStaffId(residentId);

            var proposal = _mapper.Map<Proposal>(createDto);

            proposal.ResidentId = residentId;
            proposal.OperationStaffId = staffId; // Gán Staff phụ trách nếu tìm thấy
            proposal.Status = "Pending";

            var createdProposal = await _proposalRepo.AddAsync(proposal);

            // Cần lấy lại data với join để mapper sang DTO
            var result = await _proposalRepo.GetProposalDetailsAsync(createdProposal.ProposalId);

            return _mapper.Map<ProposalDto>(result!);
        }

        // ============================
        // GET PROPOSALS (RESIDENT HISTORY)
        // ============================
        public async Task<IEnumerable<ProposalDto>> GetProposalsByResidentAsync(string residentId)
        {
            var proposals = await _proposalRepo.GetResidentProposalsAsync(residentId);
            return _mapper.Map<IEnumerable<ProposalDto>>(proposals);
        }

        // ============================
        // GET PROPOSALS (STAFF LIST)
        // ============================
        public async Task<IEnumerable<ProposalDto>> GetProposalsForStaffAsync(string staffId)
        {
            var proposals = await _proposalRepo.GetStaffAssignedProposalsAsync(staffId);
            return _mapper.Map<IEnumerable<ProposalDto>>(proposals);
        }

        // ============================
        // GET DETAIL
        // ============================
        public async Task<ProposalDto?> GetProposalDetailAsync(string proposalId, string currentUserId)
        {
            var proposal = await _proposalRepo.GetProposalDetailsAsync(proposalId);
            if (proposal == null) return null;

            // Kiểm tra quyền: Chỉ Resident sở hữu hoặc Staff được gán mới được xem
            if (proposal.ResidentId != currentUserId && proposal.OperationStaffId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem Proposal này.");
            }

            return _mapper.Map<ProposalDto>(proposal);
        }

        // ============================
        // REPLY PROPOSAL (STAFF)
        // ============================
        public async Task<ProposalDto?> ReplyProposalAsync(string proposalId, string staffId, ProposalReplyDto replyDto)
        {
            var proposal = await _proposalRepo.GetByIdAsync(proposalId);

            if (proposal == null) return null;

            // Business Rule: Chỉ Staff mới có thể trả lời
            var staffUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffId);
            var staffRoleName = staffUser?.Role.RoleName.ToLower() ?? "";

            if (!staffRoleName.Contains("staff") && staffRoleName != "admin")
            {
                throw new UnauthorizedAccessException("Chỉ Staff/Admin mới có thể trả lời đề xuất.");
            }

            // Gán Staff phụ trách nếu chưa được gán
            if (proposal.OperationStaffId == null)
            {
                proposal.OperationStaffId = staffId;
            }

            proposal.Reply = replyDto.ReplyContent;
            proposal.Status = "Completed";
            proposal.UpdatedAt = DateTime.Now;

            await _proposalRepo.UpdateAsync(proposal);

            return await GetProposalDetailAsync(proposalId, staffId);
        }
    }
}
