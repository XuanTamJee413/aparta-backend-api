using ApartaAPI.DTOs.Visitors;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitorService
    {
        Task<IEnumerable<VisitorDto>> GetAllAsync();
        Task<VisitorDto?> GetByIdAsync(string id);
        Task<VisitorDto> CreateAsync(VisitorCreateDto dto);
        Task<bool> UpdateAsync(string id, VisitorUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}