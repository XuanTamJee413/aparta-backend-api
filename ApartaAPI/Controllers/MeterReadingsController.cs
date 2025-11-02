using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Services.Interfaces;
using System.Security.Claims;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeterReadingsController : ControllerBase
{
    private readonly IMeterReadingService _service;

    public MeterReadingsController(IMeterReadingService service)
    {
        _service = service;
    }

    [HttpGet("recording-sheet")]
    [Authorize(Policy = "CanReadMeterReadingSheet")]
    public async Task<ActionResult<ApiResponse<List<ApartmentMeterInfoDto>>>> GetRecordingSheet(
        [FromQuery] string buildingCode)
    {
        try
        {
            string billingPeriod = DateTime.Now.ToString("yyyy-MM");
            var data = await _service.GetApartmentsForRecordingAsync(buildingCode, billingPeriod);
            return Ok(ApiResponse<List<ApartmentMeterInfoDto>>.Success(
                data,
                "Retrieved recording sheet successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<ApartmentMeterInfoDto>>.Fail($"Error: {ex.Message}"));
        }
    }


    [HttpPost("record")]
    [Authorize(Policy = "CanCreateMeterReading")]
    public async Task<ActionResult<ApiResponse<MeterReadingDto>>> RecordReading(
        [FromBody] RecordMeterReadingRequest request)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ?? 
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MeterReadingDto>.Fail("User ID not found in token. Please login again."));
            }

            string billingPeriod = DateTime.Now.ToString("yyyy-MM");
            var result = await _service.RecordMeterReadingAsync(request, userId, billingPeriod);

            return Ok(ApiResponse<MeterReadingDto>.Success(
                result,
                "Meter reading recorded successfully"
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<MeterReadingDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MeterReadingDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MeterReadingDto>.Fail($"Error: {ex.Message}"));
        }
    }

    [HttpGet("progress/{buildingCode}")]
    [Authorize(Policy = "CanReadMeterReadingProgress")]
    public async Task<ActionResult<ApiResponse<RecordingProgressDto>>> GetProgress(
        string buildingCode,
        [FromQuery] string? billingPeriod = null)
    {
        try
        {
            billingPeriod ??= DateTime.Now.ToString("yyyy-MM");

            if (!DateTime.TryParseExact(billingPeriod, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out _))
            {
                return BadRequest(ApiResponse<RecordingProgressDto>.Fail("Định dạng billingPeriod không hợp lệ. Sử dụng định dạng yyyy-MM"));
            }

            var result = await _service.GetRecordingProgressAsync(buildingCode, billingPeriod);

            return Ok(ApiResponse<RecordingProgressDto>.Success(
                result,
                "Lấy tiến độ ghi chỉ số thành công"
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RecordingProgressDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RecordingProgressDto>.Fail($"Lỗi: {ex.Message}"));
        }
    }


    [HttpPost("generate-invoices/{buildingCode}")]
    [Authorize(Policy = "CanCreateMeterReading")]
    public async Task<ActionResult<ApiResponse<int>>> GenerateInvoices(
        string buildingCode)
    {
        try
        {
            string billingPeriod = DateTime.Now.ToString("yyyy-MM");

            var progress = await _service.GetRecordingProgressAsync(buildingCode, billingPeriod);
            
            bool allMetersComplete = progress.ProgressByMeterType.Values.All(progressValue => progressValue >= 100);
            
            if (!allMetersComplete)
            {
                var incompleteMeters = progress.ProgressByMeterType
                    .Where(kvp => kvp.Value < 100)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value}%")
                    .ToList();
                    
                return BadRequest(ApiResponse<int>.Fail(
                    $"Không thể tạo hóa đơn. Tiến độ ghi chỉ số chưa đạt 100% cho tất cả loại đồng hồ. " +
                    $"Các đồng hồ chưa hoàn thành: {string.Join(", ", incompleteMeters)}. " +
                    $"Tổng số căn hộ: {progress.TotalApartments}, " +
                    $"Đã ghi: {string.Join(", ", progress.RecordedByMeterType.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}"
                ));
            }

            var count = await _service.GenerateMonthlyInvoicesAsync(progress.BuildingId, billingPeriod);
            
            if (count == 0)
            {
                return Ok(ApiResponse<int>.Success(
                    count,
                    "Không có hóa đơn nào được tạo. Các hóa đơn đều đã được tạo."
                ));
            }

            return Ok(ApiResponse<int>.Success(
                count,
                $"Generated {count} invoices successfully"
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<int>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<int>.Fail($"Error: {ex.Message}"));
        }
    }

    [HttpGet("recorded-readings")]
    [Authorize(Policy = "CanReadMeterReadingRecord")]
    public async Task<ActionResult<ApiResponse<List<MeterReadingDto>>>> GetRecordedReadings(
        [FromQuery] string buildingCode,
        [FromQuery] string billingPeriod)
    {
        try
        {
            if (!DateTime.TryParseExact(billingPeriod, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out _))
            {
                return BadRequest(ApiResponse<List<MeterReadingDto>>.Fail("Định dạng billingPeriod không hợp lệ. Sử dụng định dạng yyyy-MM"));
            }

            var readings = await _service.GetRecordedReadingsByPeriodAsync(buildingCode, billingPeriod);

            return Ok(ApiResponse<List<MeterReadingDto>>.Success(
                readings,
                "Lấy danh sách chỉ số điện nước thành công"
            ));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<MeterReadingDto>>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MeterReadingDto>>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    [HttpGet("history/{apartmentId}")]
    [Authorize(Policy = "CanReadMeterReadingHistory")]
    public async Task<ActionResult<ApiResponse<List<MeterReadingDto>>>> GetHistory(
        string apartmentId,
        [FromQuery] string meterId,
        [FromQuery] int limit = 12)
    {
        try
        {
            var results = await _service.GetReadingHistoryAsync(apartmentId, meterId, limit);

            return Ok(ApiResponse<List<MeterReadingDto>>.Success(
                results,
                "Retrieved reading history successfully"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MeterReadingDto>>.Fail($"Error: {ex.Message}"));
        }
    }
}

