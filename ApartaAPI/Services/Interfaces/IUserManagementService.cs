using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams);
        Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams);

        Task<ApiResponse<UserAccountDto>> CreateStaffAccountAsync(StaffCreateDto createDto);
        Task<ApiResponse<UserAccountDto>> ToggleUserStatusAsync(string userId, StatusUpdateDto dto);
        Task<ApiResponse> UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto);

        Task<ApiResponse> ResetStaffPasswordAsync(string staffId);
    }
}