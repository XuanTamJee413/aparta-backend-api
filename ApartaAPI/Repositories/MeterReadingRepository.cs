using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories;

public class MeterReadingRepository : Repository<MeterReading>, IMeterReadingRepository
{
    public MeterReadingRepository(ApartaDbContext context) : base(context) { }

    public async Task<MeterReading?> GetByApartmentAndPeriodAsync(string apartmentId, string meterId, string billingPeriod)
    {
        return await _context.MeterReadings
            .Include(mr => mr.Meter)
            .Include(mr => mr.Apartment)
            .Include(mr => mr.RecordedByUser)
            .FirstOrDefaultAsync(mr =>
                mr.ApartmentId == apartmentId &&
                mr.MeterId == meterId &&
                mr.BillingPeriod == billingPeriod);
    }

    public async Task<List<MeterReading>> GetByBuildingAndPeriodAsync(string buildingId, string billingPeriod)
    {
        return await _context.MeterReadings
            .Include(mr => mr.Meter)
            .Include(mr => mr.Apartment)
            .Include(mr => mr.RecordedByUser)
            .Where(mr =>
                mr.Apartment.BuildingId == buildingId &&
                mr.BillingPeriod == billingPeriod)
            .ToListAsync();
    }

    public async Task<MeterReading?> GetLatestReadingAsync(string apartmentId, string meterId, string beforePeriod)
    {
        return await _context.MeterReadings
            .Include(mr => mr.Meter)
            .Where(mr =>
                mr.ApartmentId == apartmentId &&
                mr.MeterId == meterId &&
                string.Compare(mr.BillingPeriod, beforePeriod) < 0)
            .OrderByDescending(mr => mr.BillingPeriod)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MeterReading>> GetReadingHistoryAsync(string apartmentId, string meterId, int limit = 12)
    {
        return await _context.MeterReadings
            .Include(mr => mr.Meter)
            .Include(mr => mr.Apartment)
            .Include(mr => mr.RecordedByUser)
            .Where(mr =>
                mr.ApartmentId == apartmentId &&
                mr.MeterId == meterId)
            .OrderByDescending(mr => mr.BillingPeriod)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountRecordedReadingsAsync(string buildingId, string billingPeriod, string meterId)
    {
        return await _context.MeterReadings
            .Include(mr => mr.Apartment)
            .Where(mr =>
                mr.Apartment.BuildingId == buildingId &&
                mr.BillingPeriod == billingPeriod &&
                mr.MeterId == meterId)
            .CountAsync();
    }
}

