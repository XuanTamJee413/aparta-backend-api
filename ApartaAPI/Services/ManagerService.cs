using ApartaAPI.Data;
using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class ManagerService : IManagerService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly ApartaDbContext _context;

        public ManagerService(
            IRepository<User> userRepository, 
            IRepository<Role> roleRepository,
            ApartaDbContext context)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _context = context;
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

            var managers = await _userRepository.FindAsync(u =>
                u.RoleId == managerRoleId && // Dùng ID đã lấy
                !u.IsDeleted &&
                (searchTerm == null ||
                    (u.Name != null && u.Name.ToLower().Contains(searchTerm)) ||
                    (u.StaffCode != null && u.StaffCode.ToLower().Contains(searchTerm)))
            );

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
                PermissionGroup = null
            }).ToList();

            return ApiResponse<IEnumerable<ManagerDto>>.Success(managerDtos);
        }

        public async Task<ApiResponse<ManagerDto>> CreateManagerAsync(CreateManagerDto dto)
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
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

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
                PermissionGroup = null
            };

            return ApiResponse<ManagerDto>.SuccessWithCode(resultDto, ApiResponse.SM04_CREATE_SUCCESS, "Manager");
        }

        public async Task<ApiResponse<ManagerDto>> UpdateManagerAsync(string userId, UpdateManagerDto dto)
        {
            var manager = await _userRepository.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
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

            await _userRepository.UpdateAsync(manager);
            await _userRepository.SaveChangesAsync();

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
                PermissionGroup = null
            };

            return ApiResponse<ManagerDto>.Success(resultDto, ApiResponse.SM03_UPDATE_SUCCESS);
        }
    }
}