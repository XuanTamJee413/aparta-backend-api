using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IManagerService
    {
        Task<ApiResponse<IEnumerable<ManagerDto>>> GetAllManagersAsync(ManagerSearch search);
        Task<ApiResponse<ManagerDto>> CreateManagerAsync(CreateManagerDto dto, string assignedBy);
        Task<ApiResponse<ManagerDto>> UpdateManagerAsync(string userId, UpdateManagerDto dto, string assignedBy);
        Task<ApiResponse<IEnumerable<ManagerBuildingOptionDto>>> GetBuildingOptionsAsync(string? managerId);
    }
}
