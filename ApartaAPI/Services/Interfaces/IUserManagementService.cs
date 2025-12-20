using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Roles;
using ApartaAPI.DTOs.User;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams, string managerId);
        Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams, string managerId);

        Task<ApiResponse<UserAccountDto>> CreateStaffAccountAsync(StaffCreateDto createDto);
        Task<ApiResponse<UserAccountDto>> ToggleUserStatusAsync(string userId, StatusUpdateDto dto);
        Task<ApiResponse> UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto);
        Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesForManagerAsync();
        Task<ApiResponse<IEnumerable<BuildingDto>>> GetManagedBuildingsAsync(string managerId);

        Task<ApiResponse> ResetStaffPasswordAsync(string staffId);
    }
}