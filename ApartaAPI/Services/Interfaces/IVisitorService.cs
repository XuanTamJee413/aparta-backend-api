using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitorService
    {
        Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto);
        Task<IEnumerable<VisitorDto>> GetRecentVisitorsAsync(string apartmentId);
    }
}