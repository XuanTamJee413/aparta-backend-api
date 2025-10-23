using ApartaAPI.DTOs.VisitLogs;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitLogService
    {
        Task<IEnumerable<VisitLogDto>> GetAllAsync();
        Task<VisitLogDto?> GetByIdAsync(string id);
        Task<VisitLogDto> CreateAsync(VisitLogCreateDto dto);
        Task<bool> UpdateAsync(string id, VisitLogUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<VisitLogHistoryDto>> GetByApartmentIdAsync(string apartmentId);
    }
}