using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
                //format: yyyy-MM
                var billingPeriod = readingDto.ReadingDate.ToString("yyyy-MM");

                // Kiểm tra xem đã tồn tại meterReading trong tháng này chưa
                var existingReading = await _meterReadingRepository.FirstOrDefaultAsync(mr =>
                    mr.ApartmentId == apartmentId &&
                    mr.FeeType == readingDto.FeeType &&
                    mr.BillingPeriod == billingPeriod);

                if (existingReading != null)
                {
                    // Đã tồn tại chỉ số trong tháng này, không được tạo mới
                    var message = ApiResponse.SM34_READING_EXISTS_IN_PERIOD
                        .Replace("{feeType}", readingDto.FeeType)
                        .Replace("{billingPeriod}", billingPeriod);
                    return ApiResponse.Fail(message);
                }

                // Kiểm tra chỉ số mới nhất trước đó (nếu có)
                // Tìm tất cả meterReadings của cùng apartment và feeType
                var allReadings = await _meterReadingRepository.FindAsync(mr =>
                    mr.ApartmentId == apartmentId &&
                    mr.FeeType == readingDto.FeeType);

                // Tìm meterReading mới nhất có billingPeriod < billingPeriod hiện tại
                var latestReading = allReadings
                    .Where(mr => !string.IsNullOrEmpty(mr.BillingPeriod) && 
                                 mr.BillingPeriod.CompareTo(billingPeriod) < 0)
                    .OrderByDescending(mr => mr.BillingPeriod)
                    .FirstOrDefault();

                if (latestReading != null)
                {
                    // Có chỉ số trước đó, kiểm tra giá trị mới phải >= giá trị chỉ số mới nhất
                    if (readingDto.ReadingValue < latestReading.ReadingValue)
                    {
                        var message = ApiResponse.SM35_READING_VALUE_TOO_LOW
                            .Replace("{newValue}", readingDto.ReadingValue.ToString("F2"))
                            .Replace("{previousValue}", latestReading.ReadingValue.ToString("F2"));
                        return ApiResponse.Fail(message);
                    }
                }

                var meterReading = new MeterReading
                {
                    MeterReadingId = Guid.NewGuid().ToString("N"),
                    ApartmentId = apartmentId,
                    FeeType = readingDto.FeeType,
                    ReadingValue = readingDto.ReadingValue,
                    ReadingDate = readingDto.ReadingDate, 
                    BillingPeriod = billingPeriod, 
                    RecordedBy = userId,
                    InvoiceItemId = null, // Để NULL
                    CreatedAt = now,
                    UpdatedAt = null
                };

                meterReadings.Add(meterReading);
            }

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

            // Kiểm tra chỉ số mới nhất trước đó (nếu có)
            if (!string.IsNullOrEmpty(meterReading.BillingPeriod))
            {
                // Tìm tất cả meterReadings của cùng apartment và feeType
                var allReadings = await _meterReadingRepository.FindAsync(mr =>
                    mr.ApartmentId == meterReading.ApartmentId &&
                    mr.FeeType == meterReading.FeeType &&
                    mr.MeterReadingId != readingId); // Loại trừ chính nó

                // Tìm meterReading mới nhất có billingPeriod < billingPeriod hiện tại
                var latestReading = allReadings
                    .Where(mr => !string.IsNullOrEmpty(mr.BillingPeriod) && 
                                 mr.BillingPeriod.CompareTo(meterReading.BillingPeriod) < 0)
                    .OrderByDescending(mr => mr.BillingPeriod)
                    .FirstOrDefault();

                if (latestReading != null)
                {
                    // Có chỉ số trước đó, kiểm tra giá trị mới phải >= giá trị chỉ số mới nhất
                    if (updateDto.ReadingValue < latestReading.ReadingValue)
                    {
                        var message = ApiResponse.SM35_READING_VALUE_TOO_LOW
                            .Replace("{newValue}", updateDto.ReadingValue.ToString("F2"))
                            .Replace("{previousValue}", latestReading.ReadingValue.ToString("F2"));
                        return ApiResponse.Fail(message);
                    }
                }
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

