using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;

namespace ApartaAPI.Services.Interfaces
{
    public interface IRoleService
    {
        Task<ApiResponse<IEnumerable<RoleDto>>> GetAllRolesAsync();
        Task<ApiResponse<RoleDto>> GetRoleByIdAsync(string roleId);
        Task<ApiResponse<RoleDto>> CreateRoleAsync(RoleCreateDto dto);
        Task<ApiResponse> UpdateRoleAsync(string roleId, RoleUpdateDto dto);
        Task<ApiResponse> DeleteRoleAsync(string roleId); // Soft delete

        Task<ApiResponse<IEnumerable<PermissionDto>>> GetAllPermissionsAsync();
        Task<ApiResponse<IEnumerable<PermissionDto>>> GetPermissionsForRoleAsync(string roleId);
        Task<ApiResponse> AssignPermissionsToRoleAsync(string roleId, PermissionAssignmentDto dto);
    }
}