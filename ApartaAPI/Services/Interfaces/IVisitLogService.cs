using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitLogService
    {
        // get all nhung da joij bang visitor va apartment
        Task<PagedList<VisitLogStaffViewDto>> GetStaffViewLogsAsync(VisitorQueryParameters queryParams);
        // check-in check-out
        Task<bool> CheckInAsync(string id);
        Task<bool> CheckOutAsync(string id);
    }
}