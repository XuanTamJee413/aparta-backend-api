using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces;

public interface IMeterReadingRepository : IRepository<MeterReading>
{
    Task<MeterReading?> GetByApartmentAndPeriodAsync(string apartmentId, string meterId, string billingPeriod);
    Task<List<MeterReading>> GetByBuildingAndPeriodAsync(string buildingId, string billingPeriod);
    Task<MeterReading?> GetLatestReadingAsync(string apartmentId, string meterId, string beforePeriod);
    Task<List<MeterReading>> GetReadingHistoryAsync(string apartmentId, string meterId, int limit = 12);
    Task<int> CountRecordedReadingsAsync(string buildingId, string billingPeriod, string meterId);
}

