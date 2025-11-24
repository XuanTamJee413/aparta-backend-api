using ApartaAPI.Data;
using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Helpers;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApartaAPI.Services
{
    public class ManagerService : IManagerService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<StaffBuildingAssignment> _staffBuildingAssignmentRepository;
        private readonly ApartaDbContext _context;
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;

        public ManagerService(
            IRepository<User> userRepository, 
            IRepository<Role> roleRepository,
            IRepository<StaffBuildingAssignment> staffBuildingAssignmentRepository,
            ApartaDbContext context,
            IMailService mailService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _staffBuildingAssignmentRepository = staffBuildingAssignmentRepository;
            _context = context;
            _mailService = mailService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<IEnumerable<ManagerDto>>> GetAllManagersAsync(ManagerSearch query)
        {
            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            // Lấy thông tin Role "manager"
            var managerRole = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == "manager");
            if (managerRole == null)
            {
                return ApiResponse<IEnumerable<ManagerDto>>.Fail("Hệ thống chưa định nghĩa Role 'manager'.");
            }
            var managerRoleId = managerRole.RoleId;
            var roleName = managerRole.RoleName;

            // Sử dụng context để Include StaffBuildingAssignments và Building
            var managersQuery = _context.Users
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .Where(u =>
                    u.RoleId == managerRoleId &&
                    !u.IsDeleted &&
                    (searchTerm == null ||
                        (u.Name != null && u.Name.ToLower().Contains(searchTerm)) ||
                        (u.StaffCode != null && u.StaffCode.ToLower().Contains(searchTerm)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm)))
                );

            var managers = await managersQuery.ToListAsync();

            if (!managers.Any())
            {
                return ApiResponse<IEnumerable<ManagerDto>>.Success(
                    new List<ManagerDto>(),
                    ApiResponse.SM01_NO_RESULTS
                );
            }

            var sortedManagers = managers.OrderByDescending(m => m.CreatedAt);

            var managerDtos = sortedManagers.Select(m => new ManagerDto
            {
                UserId = m.UserId,
                StaffCode = m.StaffCode,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                AvatarUrl = m.AvatarUrl,
                Role = roleName, // Dùng tên Role đã lấy
                Status = m.Status,
                LastLoginAt = m.LastLoginAt,
                PermissionGroup = null,
                AssignedBuildings = m.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .Select(sba => new BuildingSummaryDto
                    {
                        BuildingId = sba.BuildingId,
                        BuildingName = sba.Building.Name,
                        BuildingCode = sba.Building.BuildingCode,
                        IsActive = sba.IsActive
                    }).ToList()
            }).ToList();

            return ApiResponse<IEnumerable<ManagerDto>>.Success(managerDtos);
        }

        public async Task<ApiResponse<ManagerDto>> CreateManagerAsync(CreateManagerDto dto, string assignedBy)
        {
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Phone == dto.Phone);
            //EF2: Existing phone number
            if (existingUser != null)
            {
                return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Phone");
            }
            //EF2: Existing Email number
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var existingEmail = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingEmail != null)
                {
                    return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Email");
                }
            }
            //EF2: Existing StaffCode number
            if (!string.IsNullOrWhiteSpace(dto.StaffCode))
            {
                var existingStaffCode = await _userRepository.FirstOrDefaultAsync(u => u.StaffCode == dto.StaffCode);
                if (existingStaffCode != null)
                {
                    return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "StaffCode");
                }
            }

            // Lấy Role "manager" bằng tên
            var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleName == "manager");
            if (role == null)
            {
                return ApiResponse<ManagerDto>.Fail("Role 'manager' không tồn tại trong hệ thống");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var now = DateTime.UtcNow;
            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString("N"),
                RoleId = role.RoleId, // Gán RoleId đã lấy
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email ?? $"{dto.Phone}@aparta.com",
                PasswordHash = passwordHash,
                StaffCode = dto.StaffCode,
                AvatarUrl = dto.AvatarUrl,
                Status = "active",
                IsDeleted = false,
                IsFirstLogin = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(newUser);

            // Xử lý BuildingIds - Tạo StaffBuildingAssignment cho mỗi BuildingId
            if (dto.BuildingIds != null && dto.BuildingIds.Any())
            {
                foreach (var buildingId in dto.BuildingIds)
                {
                    var assignment = new StaffBuildingAssignment
                    {
                        UserId = newUser.UserId,
                        BuildingId = buildingId,
                        AssignmentStartDate = DateOnly.FromDateTime(now),
                        AssignmentEndDate = null,
                        ScopeOfWork = "Quản lý tòa nhà",
                        Position = "Manager",
                        AssignedBy = assignedBy,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _staffBuildingAssignmentRepository.AddAsync(assignment);
                }
            }

            // Lưu tất cả thay đổi (User và StaffBuildingAssignments)
            await _context.SaveChangesAsync();

            // Gửi email thông báo tài khoản và mật khẩu cho Manager
            var managerEmail = newUser.Email;
            if (!string.IsNullOrWhiteSpace(managerEmail))
            {
                try
                {
                    var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
                    var htmlMessage = EmailTemplateHelper.GetManagerWelcomeEmailTemplate(
                        newUser.Name ?? "Manager",
                        newUser.Phone,
                        managerEmail,
                        dto.Password,
                        frontendUrl
                    );

                    await _mailService.SendEmailAsync(
                        managerEmail,
                        "Thông tin tài khoản Manager - Aparta System",
                        htmlMessage
                    );
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không fail việc tạo manager
                    // Trong production nên log vào file hoặc logging service
                    Console.WriteLine($"Lỗi khi gửi email cho Manager: {ex.Message}");
                }
            }

            // Lấy lại manager với thông tin building assignments
            var createdManager = await _context.Users
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == newUser.UserId);

            var resultDto = new ManagerDto
            {
                UserId = newUser.UserId,
                Name = newUser.Name,
                Email = newUser.Email,
                Phone = newUser.Phone,
                StaffCode = newUser.StaffCode,
                AvatarUrl = newUser.AvatarUrl,
                Role = role.RoleName,
                Status = newUser.Status,
                LastLoginAt = newUser.LastLoginAt,
                PermissionGroup = null,
                AssignedBuildings = createdManager?.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .Select(sba => new BuildingSummaryDto
                    {
                        BuildingId = sba.BuildingId,
                        BuildingName = sba.Building.Name,
                        BuildingCode = sba.Building.BuildingCode,
                        IsActive = sba.IsActive
                    }).ToList() ?? new List<BuildingSummaryDto>()
            };

            return ApiResponse<ManagerDto>.SuccessWithCode(resultDto, ApiResponse.SM04_CREATE_SUCCESS, "Manager");
        }

        public async Task<ApiResponse<ManagerDto>> UpdateManagerAsync(string userId, UpdateManagerDto dto, string assignedBy)
        {
            // Bước 1: Tải manager với StaffBuildingAssignments
            var manager = await _context.Users
                .Include(u => u.StaffBuildingAssignmentUsers)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (manager == null)
            {
                return ApiResponse<ManagerDto>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone) && dto.Phone != manager.Phone)
            {
                var exists = await _userRepository.FirstOrDefaultAsync(u => u.Phone == dto.Phone && u.UserId != userId);
                if (exists != null)
                    return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Phone");
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != manager.Email)
            {
                var exists = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserId != userId);
                if (exists != null)
                    return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Email");
            }

            if (!string.IsNullOrWhiteSpace(dto.StaffCode) && dto.StaffCode != manager.StaffCode)
            {
                var exists = await _userRepository.FirstOrDefaultAsync(u => u.StaffCode == dto.StaffCode && u.UserId != userId);
                if (exists != null)
                    return ApiResponse<ManagerDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "StaffCode"); 
            }

            // Bước 2 & 3 & 4: Đồng bộ hóa BuildingIds
            if (dto.BuildingIds != null)
            {
                var newBuildingIds = dto.BuildingIds;
                var currentAssignments = manager.StaffBuildingAssignmentUsers.ToList();
                var currentBuildingIds = currentAssignments.Select(sba => sba.BuildingId).ToList();

                // Bước 3: Xóa các assignment không còn trong danh sách mới
                var assignmentsToRemove = currentAssignments
                    .Where(sba => !newBuildingIds.Contains(sba.BuildingId))
                    .ToList();

                foreach (var assignment in assignmentsToRemove)
                {
                    _context.StaffBuildingAssignments.Remove(assignment);
                }

                // Bước 4: Thêm các assignment mới
                var buildingIdsToAdd = newBuildingIds
                    .Where(bid => !currentBuildingIds.Contains(bid))
                    .ToList();

                var now = DateTime.UtcNow;
                foreach (var buildingId in buildingIdsToAdd)
                {
                    var newAssignment = new StaffBuildingAssignment
                    {
                        UserId = userId,
                        BuildingId = buildingId,
                        AssignmentStartDate = DateOnly.FromDateTime(now),
                        AssignmentEndDate = null,
                        ScopeOfWork = "Quản lý tòa nhà",
                        Position = "Manager",
                        AssignedBy = assignedBy,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _context.StaffBuildingAssignments.AddAsync(newAssignment);
                }
            }

            // Bước 5: Cập nhật thông tin manager
            if (!string.IsNullOrWhiteSpace(dto.Name)) manager.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Phone)) manager.Phone = dto.Phone;

            if (!string.IsNullOrWhiteSpace(dto.Email)) manager.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.StaffCode)) manager.StaffCode = dto.StaffCode;

            if (dto.AvatarUrl != null) manager.AvatarUrl = dto.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(dto.Password)) 
                manager.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Update Status (active, inactive)
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                // Check nếu manager có active assignments thì không cho chuyển sang inactive
                if (dto.Status == "inactive" && manager.Status != "inactive")
                {
                    var hasActiveAssignments = await _context.TaskAssignments
                        .Include(ta => ta.Task)
                        .AnyAsync(ta => 
                            (ta.AssigneeUserId == userId || ta.AssignerUserId == userId) &&
                            ta.Task.Status != "completed" && 
                            ta.Task.Status != "cancelled");

                    if (hasActiveAssignments)
                    {
                        return ApiResponse<ManagerDto>.Fail(ApiResponse.SM21_DELETION_FAILED);
                    }
                }

                manager.Status = dto.Status;
            }

            manager.UpdatedAt = DateTime.UtcNow;

            // Bước 6: Lưu tất cả thay đổi
            await _context.SaveChangesAsync();

            // Lấy lại manager với thông tin đầy đủ
            var updatedManager = await _context.Users
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleId == manager.RoleId);

            var resultDto = new ManagerDto
            {
                UserId = manager.UserId,
                Name = manager.Name,
                Email = manager.Email,
                Phone = manager.Phone,
                StaffCode = manager.StaffCode,
                AvatarUrl = manager.AvatarUrl,
                Role = role?.RoleName ?? "management",
                Status = manager.Status,
                LastLoginAt = manager.LastLoginAt,
                PermissionGroup = null,
                AssignedBuildings = updatedManager?.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .Select(sba => new BuildingSummaryDto
                    {
                        BuildingId = sba.BuildingId,
                        BuildingName = sba.Building.Name,
                        BuildingCode = sba.Building.BuildingCode,
                        IsActive = sba.IsActive
                    }).ToList() ?? new List<BuildingSummaryDto>()
            };

            return ApiResponse<ManagerDto>.Success(resultDto, ApiResponse.SM03_UPDATE_SUCCESS);
        }
    }
}