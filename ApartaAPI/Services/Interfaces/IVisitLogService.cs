using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitLogService
    {
        // get all tho^
        Task<IEnumerable<VisitLogDto>> GetAllAsync();
        // get all nhung da joij bang visitor va apartment
        Task<IEnumerable<VisitLogStaffViewDto>> GetStaffViewLogsAsync();
        Task<VisitLogDto?> GetByIdAsync(string id);
        Task<VisitLogDto> CreateAsync(VisitLogCreateDto dto);
        Task<bool> UpdateAsync(string id, VisitLogUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<VisitLogHistoryDto>> GetByApartmentIdAsync(string apartmentId);

        Task<VisitLogDto?> CheckInAsync(string id);
        Task<VisitLogDto?> CheckOutAsync(string id);
    }
}