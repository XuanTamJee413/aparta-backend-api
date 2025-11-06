using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeterReadingsController : ControllerBase
    {
        private readonly IMeterReadingService _service;

        public MeterReadingsController(IMeterReadingService service)
        {
            _service = service;
        }

        // Lấy danh sách các loại phí (fee_type) cân trả hằng tháng cho một căn hộ
        [HttpGet("services-for-apartment/{apartmentId}")]
        [Authorize(Policy = "CanReadMeterReadings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetServicesForApartment(string apartmentId)
        {
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
            var response = await _service.GetMeterReadingStatusByBuildingAsync(buildingId, billingPeriod);

            if (!response.Succeeded)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

