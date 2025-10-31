using ApartaAPI.DTOs.MeterReadings;

namespace ApartaAPI.Services.Interfaces;

public interface IMeterReadingService
{
    Task<List<ApartmentMeterInfoDto>> GetApartmentsForRecordingAsync(string buildingCode, string billingPeriod);
    Task<MeterReadingDto> RecordMeterReadingAsync(RecordMeterReadingRequest request, string staffId, string billingPeriod);
    Task<decimal> CalculateCostAsync(string meterId, string buildingId, int consumption);
    Task<int> GenerateMonthlyInvoicesAsync(string buildingId, string billingPeriod);
    Task<RecordingProgressDto> GetRecordingProgressAsync(string buildingCode, string billingPeriod);
    Task<List<MeterReadingDto>> GetReadingHistoryAsync(string apartmentId, string meterId, int limit = 12);
    Task<List<MeterReadingDto>> GetRecordedReadingsByPeriodAsync(string buildingId, string billingPeriod);
}

