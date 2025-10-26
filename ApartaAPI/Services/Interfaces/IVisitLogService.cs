using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitLogService
    {
        // get all nhung da joij bang visitor va apartment
        Task<IEnumerable<VisitLogStaffViewDto>> GetStaffViewLogsAsync();

        // check-in check-out
        Task<bool> CheckInAsync(string id);
        Task<bool> CheckOutAsync(string id);
    }
}