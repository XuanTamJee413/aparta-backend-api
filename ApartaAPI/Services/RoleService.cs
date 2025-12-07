using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _roleRepo;
        private readonly ApartaDbContext _context;
        private readonly IMapper _mapper;

        public RoleService(
            IRepository<Role> roleRepo,
            IRepository<Permission> permRepo,
            IRepository<PermissionGroup> permGroupRepo,
            ApartaDbContext context,
            IMapper mapper)
        {
            _roleRepo = roleRepo;
            _context = context;
            _mapper = mapper;
        }

        // --- 1. VALIDATE LOGIC ---
        private string? ValidateRoleLogic(string? roleName)
        {
            // Validate Tên Role
            if (roleName != null)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return "Tên Role không được để trống.";
                
                var cleanName = roleName.Trim();
                if (cleanName.Length < 3 || cleanName.Length > 100)
                    return "Tên Role phải từ 3 đến 100 ký tự.";
            }

            return null;
        }

        // --- Quản lý Roles ---

        public async Task<ApiResponse<IEnumerable<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _roleRepo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return ApiResponse<IEnumerable<RoleDto>>.Success(dtos);
        }

        public async Task<ApiResponse<RoleDto>> GetRoleByIdAsync(string roleId)
        {
            var role = await _roleRepo.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return ApiResponse<RoleDto>.Fail(ApiResponse.SM01_NO_RESULTS);
            }
            var dto = _mapper.Map<RoleDto>(role);
            return ApiResponse<RoleDto>.Success(dto);
        }

        public async Task<ApiResponse<RoleDto>> CreateRoleAsync(RoleCreateDto dto)
        {
            // Validate logic
            var errorMsg = ValidateRoleLogic(dto.RoleName);
            if (errorMsg != null)
            {
                return ApiResponse<RoleDto>.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            var existing = await _roleRepo.FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);
            if (existing != null)
            {
                return ApiResponse<RoleDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "RoleName");
            }

            var newRole = new Role
            {
                RoleName = dto.RoleName,
                IsSystemDefined = false, // LOGIC CUSTOM: Mọi role tạo qua API đều là Custom
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepo.AddAsync(newRole);
            await _roleRepo.SaveChangesAsync();

            var resultDto = _mapper.Map<RoleDto>(newRole);
            return ApiResponse<RoleDto>.SuccessWithCode(resultDto, ApiResponse.SM04_CREATE_SUCCESS, "Role");
        }

        public async Task<ApiResponse> UpdateRoleAsync(string roleId, RoleUpdateDto dto)
        {
            var role = await _roleRepo.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // LOGIC CUSTOM: Chỉ cho phép sửa Role Custom
            if (role.IsSystemDefined)
            {
                return ApiResponse.Fail(ApiResponse.SM24_SYSTEM_ROLES_IMMUTABLE);
            }

            // Validate logic
            var errorMsg = ValidateRoleLogic(dto.RoleName);
            if (errorMsg != null)
            {
                return ApiResponse.Fail(ApiResponse.SM25_INVALID_INPUT, null, errorMsg);
            }

            // LOGIC CUSTOM: Kiểm tra trùng lặp tên nếu đổi tên
            if (role.RoleName != dto.RoleName)
            {
                var existing = await _roleRepo.FirstOrDefaultAsync(r => r.RoleName == dto.RoleName && r.RoleId != roleId);
                if (existing != null)
                {
                    return ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "RoleName");
                }
            }

            role.RoleName = dto.RoleName;
            role.IsActive = true;
            role.UpdatedAt = DateTime.UtcNow;
            await _roleRepo.UpdateAsync(role);
            await _roleRepo.SaveChangesAsync();

            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }

        public async Task<ApiResponse> DeleteRoleAsync(string roleId)
        {
            var role = await _roleRepo.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // LOGIC CUSTOM: Không cho xóa cứng Role hệ thống
            if (role.IsSystemDefined)
            {
                return ApiResponse.Fail(ApiResponse.SM24_SYSTEM_ROLES_IMMUTABLE);
            }

            // Xóa mềm (Soft Delete)
            role.IsActive = false;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepo.UpdateAsync(role);
            await _roleRepo.SaveChangesAsync();

            return ApiResponse.SuccessWithCode(ApiResponse.SM05_DELETION_SUCCESS, "Role");
        }


        // --- Quản lý Permissions ---

        public async Task<ApiResponse<IEnumerable<PermissionDto>>> GetAllPermissionsAsync()
        {
            var permissions = await _context.Permissions
                                    .Include(p => p.PermissionGroup)
                                    .ToListAsync();

            var dtos = permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                Name = p.Name,
                GroupName = p.PermissionGroup.Name
            }).OrderBy(p => p.GroupName).ThenBy(p => p.Name);

            return ApiResponse<IEnumerable<PermissionDto>>.Success(dtos);
        }

        public async Task<ApiResponse<IEnumerable<PermissionDto>>> GetPermissionsForRoleAsync(string roleId)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .ThenInclude(p => p.PermissionGroup)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return ApiResponse<IEnumerable<PermissionDto>>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            var dtos = role.Permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                Name = p.Name,
                GroupName = p.PermissionGroup.Name
            });

            return ApiResponse<IEnumerable<PermissionDto>>.Success(dtos);
        }

        public async Task<ApiResponse> AssignPermissionsToRoleAsync(string roleId, PermissionAssignmentDto dto)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // LOGIC CUSTOM: Không cho phép sửa quyền của Role hệ thống
            //if (role.IsSystemDefined)
            //{
            //    return ApiResponse.Fail(ApiResponse.SM24_SYSTEM_ROLES_IMMUTABLE);
            //}
            // Tạm thời tắt
            // Xóa quyền cũ
            role.Permissions.Clear();

            // Thêm quyền mới
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                var newPermissions = await _context.Permissions
                    .Where(p => dto.PermissionIds.Contains(p.PermissionId))
                    .ToListAsync();

                foreach (var perm in newPermissions)
                {
                    role.Permissions.Add(perm);
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }
    }
}