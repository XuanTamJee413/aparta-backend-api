using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.User;
using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IUserManagementRepository
    {
        // Lấy danh sách Users (Staff hoặc Resident) có phân trang/tìm kiếm
        Task<PagedList<User>> GetPagedUsersAsync(UserQueryParams queryParams, List<string> rolesToInclude);

        // Lấy thông tin Assignment của Staff (để Join Building Code)
        Task<List<StaffBuildingAssignment>> GetStaffAssignmentsAsync(string userId);
    }
}
