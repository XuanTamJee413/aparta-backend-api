using ApartaAPI.DTOs.Assets;

namespace ApartaAPI.Services.Interfaces
{
    public interface IAssetService
    {
        Task<IEnumerable<AssetDto>> GetAllAsync();
        Task<AssetDto?> GetByIdAsync(string id);
        Task<AssetDto> CreateAsync(AssetCreateDto dto);
        Task<bool> UpdateAsync(string id, AssetUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
