using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Vehicles;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<ApiResponse<IEnumerable<VehicleDto>>> GetAllAsync(VehicleQueryParameters query);
        Task<VehicleDto?> GetByIdAsync(string id);
        Task<VehicleDto> CreateAsync(VehicleCreateDto dto);
        Task<bool> UpdateAsync(string id, VehicleUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<ApiResponse<IEnumerable<VehicleDto>>> GetByUserBuildingsAsync( string userId, VehicleQueryParameters query);

    }
}
