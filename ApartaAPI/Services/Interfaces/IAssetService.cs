using ApartaAPI.DTOs.Assets;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IAssetService
    {
        Task<ApiResponse<IEnumerable<AssetDto>>> GetAllAsync(AssetQueryParameters query);
        Task<AssetDto?> GetByIdAsync(string id);
        Task<AssetDto> CreateAsync(AssetCreateDto dto);
        Task<bool> UpdateAsync(string id, AssetUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<ApiResponse<IEnumerable<AssetDto>>> GetByUserBuildingsAsync(string userId,AssetQueryParameters query
);

    }
}
