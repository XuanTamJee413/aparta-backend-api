/* --- File: Services/UserManagementService.cs --- */

using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections;

namespace ApartaAPI.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _userManagementRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<StaffBuildingAssignment> _sbaRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;
        public UserManagementService(
            IUserManagementRepository userManagementRepo,
            IRepository<User> userRepo,
            IRepository<StaffBuildingAssignment> sbaRepo,
            IRepository<Role> roleRepo,
            IMapper mapper,
            ApartaDbContext context)
        {
            _userManagementRepo
                = userManagementRepo;
            _userRepo = userRepo;
            _sbaRepo = sbaRepo;
            _roleRepo = roleRepo;
            _mapper = mapper;
            _context = context;
        }

        // ===================================
        // 1. LẤY DANH SÁCH STAFF/RESIDENT (Chuyển Status về chữ thường)
        // ===================================
        private async Task<PagedList<UserAccountDto>> GetAccountsByRoleInternalAsync(UserQueryParams queryParams, List<string> roles)
        {
            var pagedUsers = await _userManagementRepo.GetPagedUsersAsync(queryParams, roles);
            var userDtos = new List<UserAccountDto>();

            foreach (var user in pagedUsers.Items)
            {
                var dto = new UserAccountDto
                {
                    UserId = user.UserId,
                    Name
                        = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleName = user.Role?.RoleName ??
                        "unknown",
                    // FIX: CHUẨN HÓA LÚC LẤY RA VỀ CHỮ THƯỜNG
                    Status = user.Status?.ToLowerInvariant() ?? "inactive",
                    StaffCode = user.StaffCode,
                    CreatedAt = user.CreatedAt,
                    IsDeleted = user.IsDeleted,

                };

                if (roles.Contains("resident")) // Resident
                {
                    // Lấy mã căn hộ cho Resident
                    if (user.ApartmentId != null)
                    {

                        var apartment = await _context.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.ApartmentId == user.ApartmentId);
                        dto.ApartmentCode = apartment?.Code;
                    }
                }
                else // Staff/Admin
                {
                    // Lấy danh sách Building Codes cho Staff

                    var assignments = await _userManagementRepo.GetStaffAssignmentsAsync(user.UserId);
                    dto.AssignedBuildingCodes = assignments.Select(a => a.Building.BuildingCode).ToList();
                }

                userDtos.Add(dto);
            }

            // PagedList<UserAccountDto> cần được tạo từ list và thông tin phân trang của pagedUsers
            return new PagedList<UserAccountDto>(userDtos, pagedUsers.TotalCount, pagedUsers.PageNumber, pagedUsers.PageSize);
        }

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams)
        {
            try
            {
                var staffRoles = new List<string> { "admin", "manager", "staff", "operation_staff", "finance_staff", "maintenance_staff", "custom" };
                var result = await GetAccountsByRoleInternalAsync(queryParams, staffRoles);

                return ApiResponse<PagedList<UserAccountDto>>.Success(result, result.TotalCount == 0 ? ApiResponse.SM01_NO_RESULTS : "Lấy danh sách nhân viên thành công.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] Lỗi hệ thống khi tải Staff: {ex.ToString()}");
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams)
        {
            try
            {
                var residentRoles = new List<string> { "resident" };
                var result = await GetAccountsByRoleInternalAsync(queryParams, residentRoles);

                return ApiResponse<PagedList<UserAccountDto>>.Success(result, result.TotalCount == 0 ? ApiResponse.SM01_NO_RESULTS : "Lấy danh sách cư dân thành công.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] Lỗi hệ thống khi tải Resident: {ex.ToString()}");
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
        }

        // ===================================
        // 2. THÊM STAFF MỚI (Chuẩn hóa Status khi lưu)
        // ===================================
        public async Task<UserAccountDto> CreateStaffAccountAsync(StaffCreateDto createDto)
        {
            // 1. Kiểm tra tồn tại Role
            var role = await _roleRepo.GetByIdAsync(createDto.RoleId);
            if (role == null) throw new ArgumentException("Role ID không hợp lệ.");
            // 2. Kiểm tra trùng Email/Phone
            if (await _userRepo.FirstOrDefaultAsync(u => u.Email == createDto.Email) != null)
            {
                throw new InvalidOperationException("Email đã tồn tại.");
            }

            if (await _userRepo.FirstOrDefaultAsync(u => u.Phone == createDto.Phone) != null)
            {
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");
            }
            // 3. Tạo User Model
            var newUser = _mapper.Map<User>(createDto);
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password);
            newUser.UserId = Guid.NewGuid().ToString();
            // FIX: LƯU TRẠNG THÁI VỀ CHỮ THƯỜNG
            newUser.Status = "active";
            newUser.IsFirstLogin = true;

            var createdUser = await _userRepo.AddAsync(newUser);
            // 4. Trả về DTO (Service sẽ chuẩn hóa trạng thái khi gọi GetAccountsByRoleInternalAsync)
            var roles = new List<string> { role.RoleName };
            var tempQueryParams = new UserQueryParams { PageNumber = 1, PageSize = 1, SearchTerm = createdUser.Phone };

            var pagedResult = await GetAccountsByRoleInternalAsync(tempQueryParams, roles);

            if (!pagedResult.Items.Any()) throw new KeyNotFoundException("User vừa tạo không tìm thấy.");

            return pagedResult.Items.First();
        }

        // ===================================
        // 3. CẬP NHẬT TRẠNG THÁI (Chuẩn hóa Status khi lưu)
        // ===================================
        public async Task<UserAccountDto> ToggleUserStatusAsync(string userId, StatusUpdateDto dto)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User không tồn tại.");

            // FIX: Chuẩn hóa input status về chữ thường
            var newStatusLower = dto.Status.ToLowerInvariant();

            if (newStatusLower != "active" && newStatusLower != "inactive")
            {
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ chấp nhận 'Active' hoặc 'Inactive'.");
            }

            // FIX: LƯU TRẠNG THÁI VỀ CHỮ THƯỜNG
            user.Status = newStatusLower;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            var updatedUserWithRole = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);

            if (updatedUserWithRole == null) throw new KeyNotFoundException("User không tồn tại.");

            var roles = new List<string> { updatedUserWithRole!.Role.RoleName };
            var tempQueryParams = new UserQueryParams { PageNumber = 1, PageSize = 1, SearchTerm = updatedUserWithRole.Phone };

            var pagedResult = await GetAccountsByRoleInternalAsync(tempQueryParams, roles);

            if (!pagedResult.Items.Any()) throw new KeyNotFoundException("User không tồn tại hoặc bị xóa.");

            return pagedResult.Items.First();
        }

        // ===================================
        // 4. CHUYỂN VỊ TRÍ STAFF (ASSIGNMENT) (Giữ nguyên)
        // ===================================
        public async System.Threading.Tasks.Task UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto)
        {
            // ... (Logic giữ nguyên)
            var staff = await _userRepo.GetByIdAsync(staffId);
            if (staff == null) throw new ArgumentException($"Staff ID '{staffId}' không tồn tại.");

            var now = DateTime.UtcNow;

            // 1. Xóa tất cả các Assignment cũ
            var oldAssignments = await _sbaRepo.FindAsync(sba => sba.UserId == staffId);
            foreach (var assignment in oldAssignments)
            {
                await _sbaRepo.RemoveAsync(assignment);
            }
            await _sbaRepo.SaveChangesAsync();

            // 2. Thêm các Assignment mới
            foreach (var buildingId in updateDto.BuildingIds)
            {
                var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
                if (building == null)
                {
                    throw new KeyNotFoundException($"Building ID '{buildingId}' không tồn tại. Không thể phân công.");
                }

                var newAssignment = new StaffBuildingAssignment
                {

                    UserId = staffId,
                    BuildingId = buildingId,
                    AssignmentStartDate = DateOnly.FromDateTime(DateTime.Today),
                    ScopeOfWork = updateDto.ScopeOfWork,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now

                };
                await _sbaRepo.AddAsync(newAssignment);
            }
            await _sbaRepo.SaveChangesAsync();
        }
    }
}