using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;

namespace ApartaAPI.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams);
        Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams);
        Task<UserAccountDto> CreateStaffAccountAsync(StaffCreateDto createDto);
        Task<UserAccountDto> ToggleUserStatusAsync(string userId, StatusUpdateDto dto);
        Task UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto);
    }
}