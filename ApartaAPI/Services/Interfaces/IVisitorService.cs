using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IVisitorService
    {
            Task<VisitorDto> CreateVisitAsync(VisitorCreateDto dto);
    }
}