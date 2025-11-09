using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;

namespace ApartaAPI.Services.Interfaces
{
    public interface IMeterReadingService
    {
        Task<ApiResponse<IEnumerable<string>>> GetServicesForApartmentAsync(string apartmentId);
        Task<ApiResponse<MeterReadingCheckResponse>> CheckMeterReadingExistsAsync(string apartmentId, string feeType, string billingPeriod);
        Task<ApiResponse> CreateMeterReadingsAsync(string apartmentId, List<MeterReadingCreateDto> readings, string userId);
        Task<ApiResponse> UpdateMeterReadingAsync(string readingId, MeterReadingUpdateDto updateDto, string? userId);
        Task<ApiResponse<IEnumerable<MeterReadingStatusDto>>> GetMeterReadingStatusByBuildingAsync(string buildingId, string? billingPeriod);
    }
}

