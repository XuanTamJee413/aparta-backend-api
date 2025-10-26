using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IManagerService
    {
        Task<ApiResponse<IEnumerable<ManagerDto>>> GetAllManagersAsync(ManagerSearch search);
        Task<ApiResponse<ManagerDto>> CreateManagerAsync(CreateManagerDto dto);
        Task<ApiResponse<ManagerDto>> UpdateManagerAsync(string userId, UpdateManagerDto dto);
    }
}
