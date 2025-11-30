using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IApartmentService
    {
        Task<ApiResponse<IEnumerable<ApartmentDto>>> GetAllAsync(ApartmentQueryParameters query);
        Task<ApartmentDto?> GetByIdAsync(string id);
        Task<ApartmentDto> CreateAsync(ApartmentCreateDto dto);
        Task<IEnumerable<ApartmentDto>> CreateBulkAsync(ApartmentBulkCreateDto dto);
        Task<bool> UpdateAsync(string id, ApartmentUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<ApiResponse<IEnumerable<ApartmentDto>>> GetByUserBuildingsAsync(string userId,ApartmentQueryParameters query
);

    }
}
