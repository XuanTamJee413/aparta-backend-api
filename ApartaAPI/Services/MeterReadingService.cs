using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
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
        private readonly IMapper _mapper;

        public MeterReadingService(
            ApartaDbContext context,
            IRepository<MeterReading> meterReadingRepository,
            IRepository<Apartment> apartmentRepository,
            IRepository<PriceQuotation> priceQuotationRepository,
            IMapper mapper)
        {
            _context = context;
            _meterReadingRepository = meterReadingRepository;
            _apartmentRepository = apartmentRepository;
            _priceQuotationRepository = priceQuotationRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<MeterReadingServiceDto>>> GetServicesForApartmentAsync(string apartmentId)
        {
            // 1. Tìm Apartment theo apartmentId
            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
            if (apartment == null)
            {
                return ApiResponse<IEnumerable<MeterReadingServiceDto>>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // 2. Lấy buildingId từ Apartment
            var buildingId = apartment.BuildingId;

            // 3. Tìm PriceQuotation với buildingId và calculation_method = "PER_UNIT_METER" hoặc "TIERED"
            var priceQuotations = await _priceQuotationRepository.FindAsync(pq =>
                pq.BuildingId == buildingId &&
                (pq.CalculationMethod == "PER_UNIT_METER" || pq.CalculationMethod == "TIERED"));

            // 4. Lấy distinct fee_type và calculation_method
            var services = priceQuotations
                .GroupBy(pq => pq.FeeType)
                .Select(g => new MeterReadingServiceDto(
                    FeeType: g.Key,
                    CalculationMethod: g.First().CalculationMethod
                ))
                .ToList();

            return ApiResponse<IEnumerable<MeterReadingServiceDto>>.Success(services);
        }

        public async Task<ApiResponse<MeterReadingCheckResponse>> CheckMeterReadingExistsAsync(string apartmentId, string feeType, string billingPeriod)
        {
            // Kiểm tra Apartment tồn tại
            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);
            if (apartment == null)
            {
                return ApiResponse<MeterReadingCheckResponse>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            // Kiểm tra xem đã tồn tại meterReading trong tháng này chưa
            var existingReading = await _meterReadingRepository.FirstOrDefaultAsync(mr =>
                mr.ApartmentId == apartmentId &&
                mr.FeeType == feeType &&
                mr.BillingPeriod == billingPeriod);

            MeterReadingDto? meterReadingDto = null;
            if (existingReading != null)
            {
                meterReadingDto = _mapper.Map<MeterReadingDto>(existingReading);
            }

            // Tìm meterReading mới nhất trước đó (nếu có)
            var allReadings = await _meterReadingRepository.FindAsync(mr =>
                mr.ApartmentId == apartmentId &&
                mr.FeeType == feeType);

            // Tìm meterReading mới nhất có billingPeriod < billingPeriod hiện tại
            var latestReading = allReadings
                .Where(mr => !string.IsNullOrEmpty(mr.BillingPeriod) && 
                             mr.BillingPeriod.CompareTo(billingPeriod) < 0)
                .OrderByDescending(mr => mr.BillingPeriod)
                .FirstOrDefault();

            MeterReadingDto? latestReadingDto = null;
            if (latestReading != null)
            {
                latestReadingDto = _mapper.Map<MeterReadingDto>(latestReading);
            }

            var response = new MeterReadingCheckResponse(
                Exists: existingReading != null,
                MeterReading: meterReadingDto,
                LatestReading: latestReadingDto
            );

            return ApiResponse<MeterReadingCheckResponse>.Success(response);
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

            // Chặn theo cửa sổ ghi số: mỗi tòa nhà có cấu hình khoảng ngày được phép ghi
            var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == apartment.BuildingId);
            if (building == null)
            {
                return ApiResponse.Fail("Không tìm thấy thông tin tòa nhà để xác định cửa sổ ghi chỉ số.");
            }

            var windowStart = building.ReadingWindowStart;
            var windowEnd = building.ReadingWindowEnd;
            var today = DateTime.Now.Day;

            // Kiểm tra xem hôm nay có trong cửa sổ ghi số không
            bool isInWindow;
            string allowedDays;
            
            if (windowStart == windowEnd)
            {
                isInWindow = today == windowStart;
                allowedDays = windowStart.ToString(CultureInfo.InvariantCulture);
            }
            else if (windowEnd > windowStart)
            {
                // Trường hợp bình thường: windowStart < windowEnd (ví dụ: 1-5)
                isInWindow = today >= windowStart && today <= windowEnd;
                allowedDays = string.Join(", ", Enumerable.Range(windowStart, windowEnd - windowStart + 1));
            }
            else
            {
                // Trường hợp cross-month: windowEnd < windowStart (ví dụ: 31 -> 1)
                // Cửa sổ từ windowStart đến cuối tháng, rồi từ đầu tháng đến windowEnd
                isInWindow = today >= windowStart || today <= windowEnd;
                var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                var days1 = Enumerable.Range(windowStart, daysInMonth - windowStart + 1);
                var days2 = Enumerable.Range(1, windowEnd);
                allowedDays = string.Join(", ", days1.Concat(days2));
            }

            if (!isInWindow)
            {
                var message = $"Lỗi: Chỉ được phép ghi chỉ số vào ngày {allowedDays} hàng tháng.";
                return ApiResponse.Fail(message);
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
                // billingPeriod = tháng trước của ReadingDate (vì ghi chỉ số cuối kỳ)
                var readingDate = readingDto.ReadingDate;
                var billingPeriod = readingDate.AddMonths(-1).ToString("yyyy-MM");

                // Sử dụng method CheckMeterReadingExistsAsync để kiểm tra
                var checkResult = await CheckMeterReadingExistsAsync(apartmentId, readingDto.FeeType, billingPeriod);
                if (!checkResult.Succeeded)
                {
                    return ApiResponse.Fail(checkResult.Message);
                }

                var checkData = checkResult.Data!;

                // Kiểm tra xem đã tồn tại meterReading trong tháng này chưa
                if (checkData.Exists && checkData.MeterReading != null)
                {
                    // Đã tồn tại chỉ số trong tháng này, không được tạo mới
                    var message = ApiResponse.SM34_READING_EXISTS_IN_PERIOD
                        .Replace("{feeType}", readingDto.FeeType)
                        .Replace("{billingPeriod}", billingPeriod);
                    return ApiResponse.Fail(message);
                }

                // Kiểm tra chỉ số mới nhất trước đó (nếu có)
                if (checkData.LatestReading != null)
                {
                    // Có chỉ số trước đó, kiểm tra giá trị mới phải >= giá trị chỉ số mới nhất
                    if (readingDto.ReadingValue < checkData.LatestReading.ReadingValue)
                    {
                        var message = ApiResponse.SM35_READING_VALUE_TOO_LOW
                            .Replace("{newValue}", readingDto.ReadingValue.ToString("F2"))
                            .Replace("{previousValue}", checkData.LatestReading.ReadingValue.ToString("F2"));
                        return ApiResponse.Fail(message);
                    }
                }

                // Sử dụng AutoMapper để tạo MeterReading từ DTO
                var meterReading = _mapper.Map<MeterReading>(readingDto);
                meterReading.MeterReadingId = Guid.NewGuid().ToString("N");
                meterReading.ApartmentId = apartmentId;
                meterReading.BillingPeriod = billingPeriod;
                meterReading.RecordedBy = userId;
                meterReading.InvoiceItemId = null; // Để NULL
                meterReading.CreatedAt = now;
                meterReading.UpdatedAt = null;

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
                // Tìm tất cả meterReadings của cùng apartment và feeType (loại trừ chính nó)
                var allReadings = await _meterReadingRepository.FindAsync(mr =>
                    mr.ApartmentId == meterReading.ApartmentId &&
                    mr.FeeType == meterReading.FeeType &&
                    mr.MeterReadingId != readingId);

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

        public async Task<ApiResponse<IEnumerable<MeterReadingStatusDto>>> GetMeterReadingStatusByBuildingAsync(string buildingId, string? billingPeriod)
        {
            try
            {
                // Bước 1: Xử lý Đầu vào
                // Xử lý Default (nếu billingPeriod là null hoặc rỗng, lấy tháng hiện tại)
                if (string.IsNullOrWhiteSpace(billingPeriod))
                {
                    billingPeriod = DateTime.Now.ToString("yyyy-MM");
                }

                // Bước 2: Tải 3 Nguồn Dữ liệu Gốc
                // Tải Căn hộ (Hàng) - TẤT CẢ căn hộ có building_id == buildingId VÀ Status == "Đã Bán" hoặc "Đang Thuê"
                var apartments = await _apartmentRepository.FindAsync(a =>
                    a.BuildingId == buildingId &&
                    (a.Status == "Đã Bán" || a.Status == "Đang Thuê"));

                if (!apartments.Any())
                {
                    return ApiResponse<IEnumerable<MeterReadingStatusDto>>.Success(
                        new List<MeterReadingStatusDto>(),
                        "Không có căn hộ nào có trạng thái 'Đã Bán' hoặc 'Đang Thuê' trong tòa nhà này."
                    );
                }

                // Tải Dịch vụ (Cột) - TẤT CẢ Price_Quotation có building_id == buildingId VÀ CalculationMethod == "PER_UNIT_METER" HOẶC "TIERED"
                var priceQuotations = await _priceQuotationRepository.FindAsync(pq =>
                    pq.BuildingId == buildingId &&
                    (pq.CalculationMethod == "PER_UNIT_METER" || pq.CalculationMethod == "TIERED"));

                var feeTypes = priceQuotations
                    .Select(pq => pq.FeeType)
                    .Distinct()
                    .ToList();

                if (!feeTypes.Any())
                {
                    return ApiResponse<IEnumerable<MeterReadingStatusDto>>.Success(
                        new List<MeterReadingStatusDto>(),
                        "Không có loại phí nào dùng đồng hồ (PER_UNIT_METER hoặc TIERED) trong tòa nhà này."
                    );
                }

                // Tải Dữ liệu (Dữ liệu đã ghi) - TẤT CẢ Meter_Reading có building_id == buildingId VÀ billingPeriod
                var meterReadings = await _context.MeterReadings
                    .Include(mr => mr.Apartment)
                    .Include(mr => mr.RecordedByNavigation) // Include User để lấy tên người ghi
                    .Where(mr =>
                        mr.Apartment.BuildingId == buildingId &&
                        mr.BillingPeriod == billingPeriod)
                    .ToListAsync();

                // Bước 3: Tạo Bảng kết quả (Trộn dữ liệu)
                var result = new List<MeterReadingStatusDto>();

                // Vòng lặp 1: Theo Căn hộ
                foreach (var apartment in apartments)
                {
                    // Vòng lặp 2: Theo Dịch vụ
                    foreach (var feeType in feeTypes)
                    {
                        // Tra cứu: Tìm kiếm trong danh sách đã ghi
                        var matchingReading = meterReadings.FirstOrDefault(mr =>
                            mr.ApartmentId == apartment.ApartmentId &&
                            mr.FeeType == feeType);

                        if (matchingReading != null)
                        {
                            // TÌM THẤY - Đã ghi
                            var status = string.IsNullOrEmpty(matchingReading.InvoiceItemId)
                                ? "Đã ghi - Mở"
                                : "Đã ghi - Đã khóa";

                            var dto = new MeterReadingStatusDto(
                                ApartmentId: apartment.ApartmentId,
                                ApartmentCode: apartment.Code,
                                FeeType: feeType,
                                ReadingValue: matchingReading.ReadingValue,
                                ReadingId: matchingReading.MeterReadingId,
                                ReadingDate: matchingReading.ReadingDate,
                                RecordedByName: matchingReading.RecordedByNavigation?.Name,
                                InvoiceItemId: matchingReading.InvoiceItemId,
                                Status: status
                            );
                            result.Add(dto);
                        }
                        else
                        {
                            // KHÔNG TÌM THẤY - Chưa ghi
                            var dto = new MeterReadingStatusDto(
                                ApartmentId: apartment.ApartmentId,
                                ApartmentCode: apartment.Code,
                                FeeType: feeType,
                                ReadingValue: null,
                                ReadingId: null,
                                ReadingDate: null,
                                RecordedByName: null,
                                InvoiceItemId: null,
                                Status: "Chưa ghi"
                            );
                            result.Add(dto);
                        }
                    }
                }

                // Bước 4: Trả về
                return ApiResponse<IEnumerable<MeterReadingStatusDto>>.Success(
                    result,
                    $"Lấy danh sách tình trạng ghi chỉ số thành công. Tổng số: {result.Count} dòng."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<MeterReadingStatusDto>>.Fail($"Lỗi khi lấy dữ liệu: {ex.Message}");
            }
        }

    }
}

