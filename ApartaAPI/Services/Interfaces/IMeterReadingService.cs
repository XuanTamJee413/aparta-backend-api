using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;

namespace ApartaAPI.Services.Interfaces
{
    public interface IMeterReadingService
    {
        Task<ApiResponse<IEnumerable<string>>> GetServicesForApartmentAsync(string apartmentId);
        Task<ApiResponse> CreateMeterReadingsAsync(string apartmentId, List<MeterReadingCreateDto> readings, string userId);
        Task<ApiResponse> UpdateMeterReadingAsync(string readingId, MeterReadingUpdateDto updateDto, string? userId);
    }
}

