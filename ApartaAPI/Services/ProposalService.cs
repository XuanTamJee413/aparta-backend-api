using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
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
            // Map dữ liệu từ DTO sang Entity
            var proposal = _mapper.Map<Proposal>(createDto);

            proposal.ResidentId = residentId;

            // LOGIC MỚI: Không tự động gán Staff. Để trống (null) để chờ Staff vào nhận.
            proposal.OperationStaffId = null;

            proposal.Status = "Pending";

            // Đảm bảo thời gian được set
            if (proposal.CreatedAt == null) proposal.CreatedAt = DateTime.UtcNow;
            if (proposal.UpdatedAt == null) proposal.UpdatedAt = DateTime.UtcNow;

            // Lưu vào Database
            var createdProposal = await _proposalRepo.AddAsync(proposal);

            // Lấy lại data đầy đủ (bao gồm thông tin Resident để hiển thị ngay)
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
        public async Task<ApiResponse<PagedList<ProposalDto>>> GetProposalsForStaffAsync(string staffId,ProposalQueryParams query)
        {
            var source = _proposalRepo.GetStaffProposalsQuery(staffId);

            // 1. Filtering by Status (combobox)
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim();
                source = source.Where(p => p.Status == status);
            }

            // 2. Filtering by SearchTerm (content search)
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLowerInvariant();
                source = source.Where(p =>
                    p.Content.ToLower().Contains(term) ||
                    p.Resident.Name.ToLower().Contains(term)
                );
            }

            // 3. Sorting (by CreatedAt and Status - yêu cầu 5)
            if (string.IsNullOrWhiteSpace(query.SortColumn))
            {
                query.SortColumn = "CreatedAt";
            }

            source = query.SortColumn.ToLowerInvariant() switch
            {
                "createdat" => query.SortDirection?.ToLowerInvariant() == "asc"
                    ? source.OrderBy(p => p.CreatedAt)
                    : source.OrderByDescending(p => p.CreatedAt),
                "status" => query.SortDirection?.ToLowerInvariant() == "asc"
                    ? source.OrderBy(p => p.Status).ThenByDescending(p => p.CreatedAt)
                    : source.OrderByDescending(p => p.Status).ThenByDescending(p => p.CreatedAt),
                _ => source.OrderByDescending(p => p.CreatedAt)
            };

            // 4. Pagination
            var totalCount = await source.CountAsync();
            var items = await source
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProposalDto>>(items);
            var pagedList = new PagedList<ProposalDto>(dtos, totalCount, query.PageNumber, query.PageSize);

            if (totalCount == 0)
            {
                return ApiResponse<PagedList<ProposalDto>>.Success(pagedList, ApiResponse.SM01_NO_RESULTS);
            }

            return ApiResponse<PagedList<ProposalDto>>.Success(pagedList);
        }

        // ============================
        // GET DETAIL
        // ============================
        public async Task<ProposalDto?> GetProposalDetailAsync(string proposalId, string currentUserId)
        {
            var proposal = await _proposalRepo.GetProposalDetailsAsync(proposalId);
            if (proposal == null) return null;

            // --- ĐOẠN CODE SỬA ĐỔI ---

            // 1. Lấy thông tin User hiện tại để check Role
            var user = await _context.Users.Include(u => u.Role)
                             .FirstOrDefaultAsync(u => u.UserId == currentUserId);
            var roleName = user?.Role.RoleName.ToLower() ?? "";

            // 2. Cho phép xem nếu:
            //    - Là chủ sở hữu (Resident)
            //    - Hoặc là người được gán (OperationStaffId)
            //    - Hoặc là nhóm quản lý (Staff/Admin/Manager) -> Cho phép xem tất cả để hỗ trợ
            bool isOwner = proposal.ResidentId == currentUserId;
            bool isAssigned = proposal.OperationStaffId == currentUserId;
            bool isManagement = roleName.Contains("staff") || roleName == "admin" || roleName == "manager";

            if (!isOwner && !isAssigned && !isManagement)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem Proposal này.");
            }
            // ---------------------------

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

            if (!staffRoleName.Contains("staff") && !staffRoleName.Contains("manager") && staffRoleName != "admin")
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
