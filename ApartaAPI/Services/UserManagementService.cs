using ApartaAPI.Data;
using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.Utils.Helper;
using AutoMapper;
using AutoMapper.QueryableExtensions; // Bắt buộc cho ProjectTo
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _userManagementRepo;
        private readonly IRepository<StaffBuildingAssignment> _sbaRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IMapper _mapper;
        private readonly ApartaDbContext _context;
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;

        public UserManagementService(
            IUserManagementRepository userManagementRepo,
            IRepository<StaffBuildingAssignment> sbaRepo,
            IRepository<Role> roleRepo,
            IMapper mapper,
            ApartaDbContext context,
            IMailService mailService,
            IConfiguration configuration)
        {
            _userManagementRepo = userManagementRepo;
            _sbaRepo = sbaRepo;
            _roleRepo = roleRepo;
            _mapper = mapper;
            _context = context;
            _mailService = mailService;
            _configuration = configuration;
        }

        // --- PRIVATE HELPER: Core Logic cho Filter, Sort, ProjectTo, Paging ---
        private async Task<PagedList<UserAccountDto>> GetAccountsByRoleInternalAsync(UserQueryParams queryParams, List<string> roles)
        {
            // 1. Nhận IQueryable từ Repository (Chưa execute)
            var query = _userManagementRepo.GetUsersQuery(roles);

            // 2. Filter (Business Logic)
            if (!string.IsNullOrWhiteSpace(queryParams.Status))
            {
                var statusFilter = queryParams.Status.Trim().ToLower();
                query = query.Where(u => u.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var term = queryParams.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Name.Contains(term) ||
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.Phone != null && u.Phone.Contains(term)) ||
                    (u.StaffCode != null && u.StaffCode.Contains(term)) ||
                    (u.Apartment != null && u.Apartment.Code.Contains(term))
                );
            }

            // 3. Sort
            if (!string.IsNullOrWhiteSpace(queryParams.SortColumn))
            {
                bool isDesc = queryParams.SortDirection?.ToLower() == "desc";
                query = queryParams.SortColumn.ToLower() switch
                {
                    "name" => isDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                    "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            // 4. PROJECTION (Tối ưu SQL: Chỉ Select cột cần thiết trong DTO)
            // Lưu ý: AutoMapper Profile cần cấu hình AssignedBuildingCodes để EF Core tự generate SubQuery/Join
            var projectQuery = query.ProjectTo<UserAccountDto>(_mapper.ConfigurationProvider);

            // 5. Paging & Execution (Thực thi query tại đây)
            var totalCount = await projectQuery.CountAsync();

            var items = await projectQuery
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return new PagedList<UserAccountDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        // --- PUBLIC METHODS ---

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams, string managerId)
        {
            try
            {
                // 1. Lấy danh sách các BuildingId mà Manager này đang quản lý
                var managedBuildingIds = await _context.StaffBuildingAssignments
                    .Where(sba => sba.UserId == managerId && sba.IsActive)
                    .Select(sba => sba.BuildingId)
                    .ToListAsync();

                // 2. Lấy danh sách UserId của tất cả nhân viên thuộc các tòa nhà đó
                var staffIdsInBuildings = await _context.StaffBuildingAssignments
                    .Where(sba => managedBuildingIds.Contains(sba.BuildingId) && sba.IsActive)
                    .Select(sba => sba.UserId)
                    .Distinct()
                    .ToListAsync();

                // 3. Khởi tạo Query lấy User theo danh sách Role nhân viên
                var staffRoles = new List<string> { "staff", "operation_staff", "finance_staff", "maintenance_staff", "custom" };
                var query = _userManagementRepo.GetUsersQuery(staffRoles);

                // 4. LỌC: Chỉ lấy những nhân viên nằm trong danh sách IDs ở bước 2
                query = query.Where(u => staffIdsInBuildings.Contains(u.UserId));

                // 5. Áp dụng các bộ lọc từ QueryParams (Search, Status)
                if (!string.IsNullOrWhiteSpace(queryParams.Status))
                {
                    var statusFilter = queryParams.Status.Trim().ToLower();
                    query = query.Where(u => u.Status == statusFilter);
                }

                if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
                {
                    var term = queryParams.SearchTerm.Trim().ToLower();
                    query = query.Where(u =>
                        u.Name.Contains(term) ||
                        (u.Email != null && u.Email.Contains(term)) ||
                        (u.Phone != null && u.Phone.Contains(term)) ||
                        (u.StaffCode != null && u.StaffCode.Contains(term))
                    );
                }

                // 6. Sắp xếp (Sorting)
                if (!string.IsNullOrWhiteSpace(queryParams.SortColumn))
                {
                    bool isDesc = queryParams.SortDirection?.ToLower() == "desc";
                    query = queryParams.SortColumn.ToLower() switch
                    {
                        "name" => isDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                        "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                        _ => query.OrderByDescending(u => u.CreatedAt)
                    };
                }
                else
                {
                    query = query.OrderByDescending(u => u.CreatedAt);
                }

                // 7. Projection sang DTO (Dùng AutoMapper để chuyển đổi dữ liệu tối ưu)
                var projectQuery = query.ProjectTo<UserAccountDto>(_mapper.ConfigurationProvider);

                // 8. Phân trang và thực thi Query
                var totalCount = await projectQuery.CountAsync();
                var items = await projectQuery
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToListAsync();

                var result = new PagedList<UserAccountDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
                return ApiResponse<PagedList<UserAccountDto>>.Success(result);
            }
            catch (Exception ex)
            {
                // Log lỗi hệ thống
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR, null, ex.Message);
            }
        }

        public async Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams, string managerId)
        {
            try
            {
                // 1. Lấy danh sách các BuildingId mà Manager này đang quản lý
                var managedBuildingIds = await _context.StaffBuildingAssignments
                    .Where(sba => sba.UserId == managerId && sba.IsActive)
                    .Select(sba => sba.BuildingId)
                    .ToListAsync();

                if (!managedBuildingIds.Any())
                {
                    return ApiResponse<PagedList<UserAccountDto>>.Success(
                        new PagedList<UserAccountDto>(new List<UserAccountDto>(), 0, queryParams.PageNumber, queryParams.PageSize)
                    );
                }

                // 2. Lấy Query gốc cho Resident
                var residentRoles = new List<string> { "resident" };
                var query = _userManagementRepo.GetUsersQuery(residentRoles);

                // 3. LỌC: Cư dân phải thuộc về các Apartment nằm trong các Building mà Manager quản lý
                // Lưu ý: User (Resident) có FK ApartmentId -> Apartment có FK BuildingId
                query = query.Where(u => u.Apartment != null && managedBuildingIds.Contains(u.Apartment.BuildingId));

                // 4. Áp dụng các bộ lọc Tìm kiếm (SearchTerm) và Trạng thái (Status)
                if (!string.IsNullOrWhiteSpace(queryParams.Status))
                {
                    var statusFilter = queryParams.Status.Trim().ToLower();
                    query = query.Where(u => u.Status == statusFilter);
                }

                if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
                {
                    var term = queryParams.SearchTerm.Trim().ToLower();
                    query = query.Where(u =>
                        u.Name.Contains(term) ||
                        (u.Email != null && u.Email.Contains(term)) ||
                        (u.Phone != null && u.Phone.Contains(term)) ||
                        (u.Apartment != null && u.Apartment.Code.Contains(term)) // Tìm theo mã căn hộ
                    );
                }

                // 5. Sắp xếp (Sorting)
                if (!string.IsNullOrWhiteSpace(queryParams.SortColumn))
                {
                    bool isDesc = queryParams.SortDirection?.ToLower() == "desc";
                    query = queryParams.SortColumn.ToLower() switch
                    {
                        "name" => isDesc ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                        "email" => isDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                        _ => query.OrderByDescending(u => u.CreatedAt)
                    };
                }
                else
                {
                    query = query.OrderByDescending(u => u.CreatedAt);
                }

                // 6. Projection và Phân trang (Sử dụng AutoMapper ProjectTo để tối ưu SQL)
                var projectQuery = query.ProjectTo<UserAccountDto>(_mapper.ConfigurationProvider);

                var totalCount = await projectQuery.CountAsync();
                var items = await projectQuery
                    .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .ToListAsync();

                var result = new PagedList<UserAccountDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
                return ApiResponse<PagedList<UserAccountDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedList<UserAccountDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR, null, ex.Message);
            }
        }

        public async Task<ApiResponse<UserAccountDto>> CreateStaffAccountAsync(StaffCreateDto createDto)
        {
            // Validate Logic
            var role = await _roleRepo.GetByIdAsync(createDto.RoleId);
            if (role == null) return ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT, "RoleId", "Role không tồn tại.");

            if (await _userManagementRepo.FirstOrDefaultAsync(u => u.Email == createDto.Email) != null)
                return ApiResponse<UserAccountDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Email");

            if (await _userManagementRepo.FirstOrDefaultAsync(u => u.Phone == createDto.Phone) != null)
                return ApiResponse<UserAccountDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Phone");

            // Mapping & Init
            var newUser = _mapper.Map<User>(createDto);
            newUser.UserId = Guid.NewGuid().ToString();
            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password);
            newUser.Status = "active";
            newUser.IsFirstLogin = true;
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            // Save DB
            await _userManagementRepo.AddAsync(newUser);

            // Gửi email (Optional)
            if (!string.IsNullOrWhiteSpace(newUser.Email))
            {
                try
                {
                    var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
                    var htmlMessage = EmailTemplateHelper.GetStaffWelcomeEmailTemplate(
                        newUser.Name, newUser.Phone ?? "", newUser.Email, createDto.Password, frontendUrl
                    );
                    await _mailService.SendEmailAsync(newUser.Email, "Thông tin tài khoản Nhân viên - Aparta System", htmlMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warning] Cannot send email: {ex.Message}");
                }
            }

            // Return DTO
            var resultDto = _mapper.Map<UserAccountDto>(newUser);
            resultDto.RoleName = role.RoleName; // Map tay vì User chưa load relation
            return ApiResponse<UserAccountDto>.SuccessWithCode(resultDto, ApiResponse.SM04_CREATE_SUCCESS, "Nhân viên");
        }

        public async Task<ApiResponse<UserAccountDto>> ToggleUserStatusAsync(string userId, StatusUpdateDto dto)
        {
            var user = await _userManagementRepo.GetByIdAsync(userId);
            if (user == null) return ApiResponse<UserAccountDto>.Fail(ApiResponse.SM01_NO_RESULTS);

            var newStatus = dto.Status.Trim().ToLowerInvariant();
            if (newStatus != "active" && newStatus != "inactive")
                return ApiResponse<UserAccountDto>.Fail(ApiResponse.SM25_INVALID_INPUT, "Status", "Trạng thái chỉ chấp nhận 'Active' hoặc 'Inactive'.");

            user.Status = newStatus;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManagementRepo.UpdateAsync(user);

            // Fetch lại để có Role info cho Mapper
            var userWithRole = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            return ApiResponse<UserAccountDto>.Success(_mapper.Map<UserAccountDto>(userWithRole), ApiResponse.SM03_UPDATE_SUCCESS);
        }

        public async Task<ApiResponse> UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto)
        {
            var staff = await _userManagementRepo.GetByIdAsync(staffId);
            if (staff == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa assignments cũ
                var oldAssignments = await _sbaRepo.FindAsync(sba => sba.UserId == staffId);
                foreach (var item in oldAssignments)
                {
                    await _sbaRepo.RemoveAsync(item);
                }

                // 2. Thêm assignments mới
                var now = DateTime.UtcNow;
                foreach (var buildingId in updateDto.BuildingIds)
                {
                    var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
                    if (building == null) throw new InvalidOperationException($"Building ID '{buildingId}' không tồn tại.");

                    var newAssignment = new StaffBuildingAssignment
                    {
                        AssignmentId = Guid.NewGuid().ToString(),
                        UserId = staffId,
                        BuildingId = buildingId,
                        AssignmentStartDate = DateOnly.FromDateTime(now),
                        ScopeOfWork = updateDto.ScopeOfWork,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _sbaRepo.AddAsync(newAssignment);
                }

                await _sbaRepo.SaveChangesAsync();
                await transaction.CommitAsync();
                return ApiResponse.Success(ApiResponse.SM06_TASK_ASSIGN_SUCCESS);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse.Fail(ApiResponse.SM40_SYSTEM_ERROR, null, ex.Message);
            }
        }

        public async Task<ApiResponse> ResetStaffPasswordAsync(string staffId)
        {
            // 1. Kiểm tra User tồn tại
            // Sử dụng _userManagementRepo (đã kế thừa Repository<User>) để lấy user
            var user = await _userManagementRepo.GetByIdAsync(staffId);
            if (user == null || user.IsDeleted)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS, null, "Nhân viên không tồn tại.");
            }

            // 2. Sinh mật khẩu ngẫu nhiên (6 ký tự: Chữ + Số)
            var newPassword = GenerateRandomPassword(6);

            // 3. Hash mật khẩu và cập nhật
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.IsFirstLogin = true; 

            await _userManagementRepo.UpdateAsync(user);
            // Lưu ý: UpdateAsync trong Repository generic thường chưa SaveChanges ngay nếu bạn không gọi, 
            // nhưng ở đây ta gọi repo.UpdateAsync (đã có SaveChanges bên trong) hoặc gọi SaveChangesAsync rời.
            // Dựa trên code Repository<T> cũ của bạn, UpdateAsync đã bao gồm SaveChanges. 
            // Nếu chưa, hãy gọi await _userManagementRepo.SaveChangesAsync();

            // 4. Gửi email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
                    var emailBody = EmailTemplateHelper.GetStaffPasswordResetEmailTemplate(
                        user.Name,
                        newPassword,
                        frontendUrl
                    );

                    await _mailService.SendEmailAsync(
                        user.Email,
                        "[Aparta] Cấp lại mật khẩu nhân viên",
                        emailBody
                    );
                }
                catch (Exception ex)
                {
                    // Log lỗi email nhưng vẫn trả về thành công vì DB đã update
                    Console.WriteLine($"[Error] Failed to send reset password email: {ex.Message}");
                    return ApiResponse.Success("Mật khẩu đã được đặt lại, nhưng gửi email thất bại. Mật khẩu mới là: " + newPassword);
                }
            }
            else
            {
                return ApiResponse.Success($"Mật khẩu đã được đặt lại thành công. (User không có email). Mật khẩu mới: {newPassword}");
            }

            return ApiResponse.Success("Đặt lại mật khẩu thành công. Mật khẩu mới đã được gửi qua email.");
        }
        public async Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesForManagerAsync()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive == true)
                .ToListAsync();

            // Lọc bỏ role admin và manager (không phân biệt hoa thường)
            var filteredRoles = roles
                .Where(r => !r.RoleName.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
                            !r.RoleName.Equals("manager", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var result = _mapper.Map<IEnumerable<RoleDto>>(filteredRoles);

            return ApiResponse<IEnumerable<RoleDto>>.Success(result);
        }
        public async Task<ApiResponse<IEnumerable<BuildingDto>>> GetManagedBuildingsAsync(string managerId)
        {
            var buildingIds = await _context.StaffBuildingAssignments
                .Where(sba => sba.UserId == managerId && sba.IsActive)
                .Select(sba => sba.BuildingId).Distinct().ToListAsync();

            if (!buildingIds.Any())
                return ApiResponse<IEnumerable<BuildingDto>>.Success(new List<BuildingDto>(), ApiResponse.SM01_NO_RESULTS);

            var buildings = await _context.Buildings
                .Where(b => buildingIds.Contains(b.BuildingId) && b.IsActive)
                .Select(b => new BuildingDto(
                    b.BuildingId, b.ProjectId, b.BuildingCode, b.Name,
                    0, 0, 0, 0, 0, null, null, null, null, 0, 0, 
                    b.CreatedAt, b.UpdatedAt, b.IsActive
                )).ToListAsync();

            return ApiResponse<IEnumerable<BuildingDto>>.Success(buildings);
        }        // Helper sinh mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}