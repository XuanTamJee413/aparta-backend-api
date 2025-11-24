using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.StaffAssignments;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class StaffAssignmentService : IStaffAssignmentService
    {
        private readonly IStaffAssignmentRepository _assignmentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Building> _buildingRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IMapper _mapper;

        public StaffAssignmentService(
            IStaffAssignmentRepository assignmentRepo,
            IRepository<User> userRepo,
            IRepository<Building> buildingRepo,
            IRepository<Role> roleRepo,
            IMapper mapper)
        {
            _assignmentRepo = assignmentRepo;
            _userRepo = userRepo;
            _buildingRepo = buildingRepo;
            _roleRepo = roleRepo;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PaginatedResult<StaffAssignmentDto>>> GetAssignmentsAsync(StaffAssignmentQueryParameters query)
        {
            var source = _assignmentRepo.GetAssignmentsQuery();

            source = source.Where(x =>
                x.User.Role.RoleName != "admin" &&
                x.User.Role.RoleName != "manager" &&
                x.User.Role.RoleName != "resident"
            );

            // 1. [MỚI] Xử lý Search Term
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                source = source.Where(x =>
                    EF.Functions.Collate(x.User.Name, "SQL_Latin1_General_CP1_CI_AI").Contains(term) ||
                    (x.User.StaffCode != null && EF.Functions.Collate(x.User.StaffCode, "SQL_Latin1_General_CP1_CI_AI").Contains(term)) ||
                    (x.Position != null && EF.Functions.Collate(x.Position, "SQL_Latin1_General_CP1_CI_AI").Contains(term))
                );
            }

            // 2. Các filter cũ
            if (!string.IsNullOrEmpty(query.BuildingId))
                source = source.Where(x => x.BuildingId == query.BuildingId);

            if (!string.IsNullOrEmpty(query.UserId))
                source = source.Where(x => x.UserId == query.UserId);

            if (query.IsActive.HasValue)
                source = source.Where(x => x.IsActive == query.IsActive.Value);

            // Sort mặc định
            source = source.OrderByDescending(x => x.CreatedAt);

            var totalCount = await source.CountAsync();
            var items = await source
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ProjectTo<StaffAssignmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var pagedResult = new PaginatedResult<StaffAssignmentDto>(items, totalCount);

            if (totalCount == 0)
                return ApiResponse<PaginatedResult<StaffAssignmentDto>>.Success(pagedResult, ApiResponse.SM01_NO_RESULTS);

            return ApiResponse<PaginatedResult<StaffAssignmentDto>>.Success(pagedResult);
        }

        public async Task<ApiResponse<StaffAssignmentDto>> AssignStaffAsync(StaffAssignmentCreateDto dto, string managerId)
        {
            // 1. Validate Building
            var building = await _buildingRepo.GetByIdAsync(dto.BuildingId);
            if (building == null)
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM01_NO_RESULTS); // Not found

            // 2. Validate User & Role (LOGIC MỚI)
            var user = await _userRepo.GetByIdAsync(dto.UserId);
            if (user == null)
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND);

            var role = await _roleRepo.GetByIdAsync(user.RoleId);
            if (role == null)
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM01_NO_RESULTS);

            var roleName = role.RoleName.Trim().ToLower();
            // Logic: Nếu KHÔNG PHẢI (admin, manager, resident) => Là STAFF (bao gồm maintenance_staff, security, cleaner, custom_role...)
            if (roleName == "admin" || roleName == "manager" || roleName == "resident")
            {
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM43_NOT_STAFF);
            }

            // 3. Validate Overlap
            var activeAssignment = await _assignmentRepo.GetActiveAssignmentAsync(dto.UserId, dto.BuildingId);
            if (activeAssignment != null)
            {
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM42_ASSIGNMENT_OVERLAP);
            }

            // 4. Create
            var entity = new StaffBuildingAssignment
            {
                AssignmentId = Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                BuildingId = dto.BuildingId,
                Position = dto.Position,
                ScopeOfWork = dto.ScopeOfWork,
                AssignmentStartDate = dto.StartDate,
                IsActive = true,
                AssignedBy = managerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _assignmentRepo.AddAsync(entity);

            // Return
            var resultDto = await GetAssignmentDtoById(entity.AssignmentId);
            return ApiResponse<StaffAssignmentDto>.SuccessWithCode(resultDto!, ApiResponse.SM06_TASK_ASSIGN_SUCCESS); // Tái sử dụng SM06 (Giao nhiệm vụ ~ Giao việc) hoặc SM04
        }

        public async Task<ApiResponse<StaffAssignmentDto>> UpdateAssignmentAsync(string assignmentId, StaffAssignmentUpdateDto dto, string managerId)
        {
            var entity = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (entity == null)
                return ApiResponse<StaffAssignmentDto>.Fail(ApiResponse.SM01_NO_RESULTS);

            bool hasChanges = false;

            if (dto.Position != null && entity.Position != dto.Position) { entity.Position = dto.Position; hasChanges = true; }
            if (dto.ScopeOfWork != null && entity.ScopeOfWork != dto.ScopeOfWork) { entity.ScopeOfWork = dto.ScopeOfWork; hasChanges = true; }

            // Logic cập nhật ngày kết thúc/trạng thái
            if (dto.EndDate.HasValue)
            {
                if (entity.AssignmentEndDate != dto.EndDate) { entity.AssignmentEndDate = dto.EndDate; hasChanges = true; }
            }

            if (entity.IsActive != dto.IsActive) { entity.IsActive = dto.IsActive; hasChanges = true; }

            if (!hasChanges) return ApiResponse<StaffAssignmentDto>.Success(null!, ApiResponse.SM20_NO_CHANGES);

            entity.UpdatedAt = DateTime.UtcNow;
            entity.AssignedBy = managerId;

            await _assignmentRepo.UpdateAsync(entity);

            var resultDto = await GetAssignmentDtoById(entity.AssignmentId);
            return ApiResponse<StaffAssignmentDto>.Success(resultDto!, ApiResponse.SM03_UPDATE_SUCCESS);
        }

        // Xóa mềm (Soft Delete) - Dành cho Manager
        public async Task<ApiResponse> DeactivateAssignmentAsync(string assignmentId)
        {
            var entity = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (entity == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

            if (!entity.IsActive) return ApiResponse.Success(ApiResponse.SM20_NO_CHANGES); // Đã inactive rồi

            entity.IsActive = false;
            entity.AssignmentEndDate = DateOnly.FromDateTime(DateTime.Now); // Đặt ngày kết thúc là hôm nay
            entity.UpdatedAt = DateTime.UtcNow;

            await _assignmentRepo.UpdateAsync(entity);

            // Tái sử dụng SM03 (Cập nhật thành công) vì bản chất là update
            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }

        // Xóa cứng (Hard Delete) - Dành cho Admin
        public async Task<ApiResponse> DeletePermanentAsync(string assignmentId)
        {
            var entity = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (entity == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

            await _assignmentRepo.RemoveAsync(entity);
            await _assignmentRepo.SaveChangesAsync();

            return ApiResponse.SuccessWithCode(ApiResponse.SM05_DELETION_SUCCESS, "Phân công");
        }

        public async Task<ApiResponse<IEnumerable<StaffUserDto>>> GetAvailableStaffsAsync(string? searchTerm)
        {
            // 1. Lấy tất cả Role để xác định Role nào bị loại trừ
            // (Lý do: Generic Repo thường khó Join/Include phức tạp, nên ta lấy Role ID trước)
            var allRoles = await _roleRepo.GetAllAsync();

            // Xác định các Role không phải là Staff (Admin, Manager, Resident)
            var excludedRoleIds = allRoles
                .Where(r =>
                    r.RoleName.Trim().ToLower() == "admin" ||
                    r.RoleName.Trim().ToLower() == "manager" ||
                    r.RoleName.Trim().ToLower() == "resident")
                .Select(r => r.RoleId)
                .ToList();

            // Tạo Dictionary để map RoleId -> RoleName nhanh chóng sau này
            var roleMap = allRoles.ToDictionary(r => r.RoleId, r => r.RoleName);

            // 2. Query User từ Repository
            // Điều kiện: RoleId KHÔNG nằm trong danh sách loại trừ VÀ chưa bị xóa
            var staffUsers = await _userRepo.FindAsync(u =>
                !excludedRoleIds.Contains(u.RoleId) &&
                !u.IsDeleted && u.Status == "active");

            // 3. Lọc theo SearchTerm (nếu có) - Xử lý trên Memory nếu Repo trả về IEnumerable
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerTerm = searchTerm.Trim().ToLower();
                staffUsers = staffUsers.Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(lowerTerm)) ||
                    (u.StaffCode != null && u.StaffCode.ToLower().Contains(lowerTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(lowerTerm))
                );
            }

            staffUsers = staffUsers.OrderBy(u => u.Name);

            // 4. Map sang DTO
            var result = staffUsers.Select(u => new StaffUserDto
            {
                UserId = u.UserId,
                Name = u.Name,
                StaffCode = u.StaffCode ?? "Unknown",
                Email = u.Email ?? "Unknown",
                RoleName = roleMap.ContainsKey(u.RoleId) ? roleMap[u.RoleId] : "Unknown"
            }).ToList();

            return ApiResponse<IEnumerable<StaffUserDto>>.Success(result);
        }

        public async Task<ApiResponse<IEnumerable<StaffAssignmentBuildingDto>>> GetAvailableBuildingsAsync(string? searchTerm)
        {
            // 1. Query Building từ Repository (Chỉ lấy ACTIVE)
            // Giả sử FindAsync trả về IQueryable hoặc IEnumerable
            var buildingsQuery = await _buildingRepo.FindAsync(b => b.IsActive == true);
            var buildings = buildingsQuery.AsQueryable();

            // 2. Lọc theo SearchTerm (Tên hoặc Mã)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                buildings = buildings.Where(b =>
                    EF.Functions.Collate(b.Name, "SQL_Latin1_General_CP1_CI_AI").Contains(term) ||
                    EF.Functions.Collate(b.BuildingCode, "SQL_Latin1_General_CP1_CI_AI").Contains(term)
                );
            }

            // 3. Sắp xếp A-Z
            buildings = buildings.OrderBy(b => b.Name);

            // 4. Map sang DTO mới
            var result = buildings.Select(b => new StaffAssignmentBuildingDto
            {
                BuildingId = b.BuildingId,
                Name = b.Name,
                BuildingCode = b.BuildingCode
            }).ToList();

            return ApiResponse<IEnumerable<StaffAssignmentBuildingDto>>.Success(result);
        }

        private async Task<StaffAssignmentDto?> GetAssignmentDtoById(string id)
        {
            return await _assignmentRepo.GetAssignmentsQuery()
                .Where(x => x.AssignmentId == id)
                .ProjectTo<StaffAssignmentDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
    }
}