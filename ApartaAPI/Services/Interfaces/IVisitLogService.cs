using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitLogService
    {
        // get all nhung da joij bang visitor va apartment
        Task<PagedList<VisitLogStaffViewDto>> GetStaffViewLogsAsync(VisitorQueryParameters queryParams, string userId);

        Task<PagedList<VisitLogStaffViewDto>> GetResidentHistoryAsync(VisitorQueryParameters queryParams, string userId);
        // check-in check-out
        Task<bool> CheckInAsync(string id);
        Task<bool> CheckOutAsync(string id);
        Task<bool> RejectAsync(string id);
        Task<bool> DeleteLogAsync(string id);
        Task<bool> UpdateLogAsync(string id, VisitLogUpdateDto dto);

        // Visitor
        Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto);
        Task<VisitorCheckResponseDto> CheckVisitorExistAsync(string idNumber);
        Task<IEnumerable<VisitorDto>> GetRecentVisitorsAsync(string apartmentId);
    }
}