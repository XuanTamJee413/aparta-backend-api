using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApartaAPI.Services
{
    public class MeterReadingService : IMeterReadingService
    {
        private readonly ApartaDbContext _context;
        private readonly IRepository<MeterReading> _meterReadingRepository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly IRepository<PriceQuotation> _priceQuotationRepository;

        public MeterReadingService(
            ApartaDbContext context,
            IRepository<MeterReading> meterReadingRepository,
            IRepository<Apartment> apartmentRepository,
            IRepository<PriceQuotation> priceQuotationRepository)
        {
            _context = context;
            _meterReadingRepository = meterReadingRepository;
            _apartmentRepository = apartmentRepository;
            _priceQuotationRepository = priceQuotationRepository;
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetServicesForApartmentAsync(string apartmentId)
        {
            // 1. Tìm Apartment theo apartmentId
            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
            if (apartment == null)
            {
                return ApiResponse<IEnumerable<string>>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // 2. Lấy buildingId từ Apartment
            var buildingId = apartment.BuildingId;

            // 3. Tìm PriceQuotation với buildingId và calculation_method = "PER_UNIT_METER"
            var priceQuotations = await _priceQuotationRepository.FindAsync(pq =>
                pq.BuildingId == buildingId &&
                pq.CalculationMethod == "PER_UNIT_METER");

            // 4. Lấy distinct fee_type
            var feeTypes = priceQuotations
                .Select(pq => pq.FeeType)
                .Distinct()
                .ToList();

            return ApiResponse<IEnumerable<string>>.Success(feeTypes);
        }

        public async Task<ApiResponse> CreateMeterReadingsAsync(string apartmentId, List<MeterReadingCreateDto> readings, string userId)
        {
            if (readings == null || !readings.Any())
            {
                return ApiResponse.Fail(ApiResponse.SM31_READING_LIST_EMPTY);
            }

            // Kiểm tra Apartment tồn tại
            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
            if (apartment == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            if (string.IsNullOrEmpty(userId))
            {
                return ApiResponse.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            var now = DateTime.UtcNow;
            var meterReadings = new List<MeterReading>();

            // Tạo danh sách các MeterReading
            foreach (var readingDto in readings)
            {
                var meterReading = new MeterReading
                {
                    MeterReadingId = Guid.NewGuid().ToString("N"),
                    ApartmentId = apartmentId,
                    FeeType = readingDto.FeeType,
                    ReadingValue = readingDto.ReadingValue,
                    ReadingDate = readingDto.ReadingDate,
                    RecordedBy = userId,
                    InvoiceItemId = null, // Rất quan trọng! Để NULL
                    CreatedAt = now,
                    UpdatedAt = null
                };

                meterReadings.Add(meterReading);
            }

            // Batch insert: Thêm tất cả vào database một lần
            await _context.Set<MeterReading>().AddRangeAsync(meterReadings);
            await _context.SaveChangesAsync();

            return ApiResponse.SuccessWithCode(ApiResponse.SM33_METER_READING_CREATE_SUCCESS, null, meterReadings.Count);
        }

        public async Task<ApiResponse> UpdateMeterReadingAsync(string readingId, MeterReadingUpdateDto updateDto, string? userId)
        {
            if (updateDto == null)
            {
                return ApiResponse.Fail(ApiResponse.SM32_READING_UPDATE_EMPTY);
            }

            // Tìm MeterReading theo readingId
            var meterReading = await _meterReadingRepository.FirstOrDefaultAsync(mr => mr.MeterReadingId == readingId);
            if (meterReading == null)
            {
                return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // Kiểm tra an toàn: invoice_item_id phải là NULL
            if (!string.IsNullOrEmpty(meterReading.InvoiceItemId))
            {
                return ApiResponse.Fail(ApiResponse.SM30_READING_LOCKED);
            }

            // Cập nhật giá trị
            meterReading.ReadingValue = updateDto.ReadingValue;
            meterReading.UpdatedAt = DateTime.UtcNow;

            // Có thể cập nhật recorded_by nếu cần
            if (!string.IsNullOrEmpty(userId))
            {
                meterReading.RecordedBy = userId;
            }

            // Lưu thay đổi
            await _meterReadingRepository.UpdateAsync(meterReading);
            await _meterReadingRepository.SaveChangesAsync();

            return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
        }
    }
}

