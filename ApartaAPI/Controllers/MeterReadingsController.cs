using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Linq;
using ApartaAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMeterReadingService _service;
        private readonly ApartaDbContext _context;

        public MeterReadingsController(IMeterReadingService service, ApartaDbContext context)
        {
            _service = service;
            _context = context;
        }

        // Lấy danh sách các loại phí (fee_type) cân trả hằng tháng cho một căn hộ
        [HttpGet("services-for-apartment/{apartmentId}")]
        [Authorize(Policy = "CanReadMeterReadings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MeterReadingServiceDto>>>> GetServicesForApartment(string apartmentId)
        {
            var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<IEnumerable<MeterReadingServiceDto>>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được xem apartment của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Lấy buildingId từ apartmentId
                var apartment = await _context.Apartments
                    .Where(a => a.ApartmentId == apartmentId)
                    .Select(a => a.BuildingId)
                    .FirstOrDefaultAsync();

                if (apartment == null)
                {
                    return NotFound(ApiResponse<IEnumerable<MeterReadingServiceDto>>.Fail(ApiResponse.SM01_NO_RESULTS));
                }

                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == apartment 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập apartment này
                }
            }

            var response = await _service.GetServicesForApartmentAsync(apartmentId);

            if (!response.Succeeded)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// Kiểm tra xem có meterReading trong tháng này chưa
        [HttpGet("check/{apartmentId}/{feeType}/{billingPeriod}")]
        [Authorize(Policy = "CanReadMeterReadings")]
        public async Task<ActionResult<ApiResponse<MeterReadingCheckResponse>>> CheckMeterReadingExists(
            string apartmentId, 
            string feeType, 
            string billingPeriod)
        {
            var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MeterReadingCheckResponse>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được xem apartment của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Lấy buildingId từ apartmentId
                var apartment = await _context.Apartments
                    .Where(a => a.ApartmentId == apartmentId)
                    .Select(a => a.BuildingId)
                    .FirstOrDefaultAsync();

                if (apartment == null)
                {
                    return NotFound(ApiResponse<MeterReadingCheckResponse>.Fail(ApiResponse.SM01_NO_RESULTS));
                }

                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == apartment 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập apartment này
                }
            }

            var response = await _service.CheckMeterReadingExistsAsync(apartmentId, feeType, billingPeriod);

            if (!response.Succeeded)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        // Thêm các chỉ số mới cho một căn hộ
        [HttpPost("for-apartment/{apartmentId}")]
        [Authorize(Policy = "CanCreateMeterReadings")]
        public async Task<ActionResult<ApiResponse>> CreateMeterReadings(
            string apartmentId,
            [FromBody] List<MeterReadingCreateDto> readings)
        {
            if (readings == null || !readings.Any())
            {
                return BadRequest(ApiResponse.Fail(ApiResponse.SM31_READING_LIST_EMPTY));
            }

            // Lấy user_id của người đang đăng nhập
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được tạo meter reading cho apartment của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Lấy buildingId từ apartmentId
                var apartment = await _context.Apartments
                    .Where(a => a.ApartmentId == apartmentId)
                    .Select(a => a.BuildingId)
                    .FirstOrDefaultAsync();

                if (apartment == null)
                {
                    return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
                }

                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == apartment 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập apartment này
                }
            }

            var response = await _service.CreateMeterReadingsAsync(apartmentId, readings, userId);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Created(string.Empty, response);
        }

        // Sửa một chỉ số đã ghi (chỉ khi chưa bị khóa - invoice_item_id = null)
        [HttpPut("{readingId}")]
        [Authorize(Policy = "CanUpdateMeterReadings")]
        public async Task<ActionResult<ApiResponse>> UpdateMeterReading(
            string readingId,
            [FromBody] MeterReadingUpdateDto updateDto)
        {
            if (updateDto == null)
            {
                return BadRequest(ApiResponse.Fail(ApiResponse.SM32_READING_UPDATE_EMPTY));
            }

            // Lấy user_id của người đang đăng nhập (optional)
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được sửa meter reading của apartment thuộc building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Lấy buildingId từ readingId thông qua apartment
                var reading = await _context.MeterReadings
                    .Include(mr => mr.Apartment)
                    .Where(mr => mr.MeterReadingId == readingId)
                    .Select(mr => mr.Apartment.BuildingId)
                    .FirstOrDefaultAsync();

                if (reading == null)
                {
                    return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
                }

                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == reading 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập meter reading này
                }
            }

            var response = await _service.UpdateMeterReadingAsync(readingId, updateDto, userId);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM01_NO_RESULTS)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        // Xem tình trạng ghi chỉ số theo tòa nhà
        [HttpGet("by-building/{buildingId}")]
        [Authorize(Policy = "CanReadMeterReadingStatus")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MeterReadingStatusDto>>>> GetMeterReadingStatusByBuilding(
            string buildingId,
            [FromQuery] string? billingPeriod = null)
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<IEnumerable<MeterReadingStatusDto>>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được xem meter reading của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == buildingId 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập building này
                }
            }

            var response = await _service.GetMeterReadingStatusByBuildingAsync(buildingId, billingPeriod);

            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

