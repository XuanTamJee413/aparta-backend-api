using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;

namespace ApartaAPI.Services.Interfaces
{
    public interface IUserManagementService
    {
        // Chữ ký mới trả về ApiResponse<PagedList<T>>
        Task<ApiResponse<PagedList<UserAccountDto>>> GetStaffAccountsAsync(UserQueryParams queryParams);
        Task<ApiResponse<PagedList<UserAccountDto>>> GetResidentAccountsAsync(UserQueryParams queryParams);

        // Chữ ký mới trả về ApiResponse<T> và cho phép service ném exception
        Task<UserAccountDto> CreateStaffAccountAsync(StaffCreateDto createDto);
        Task<UserAccountDto> ToggleUserStatusAsync(string userId, StatusUpdateDto dto);

        // Chữ ký mới cho phép service ném exception
        Task UpdateStaffAssignmentAsync(string staffId, AssignmentUpdateDto updateDto);
    }
}
