using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.MeterReadings;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class MeterReadingsController : ControllerBase
{
    private readonly IMeterReadingService _service;

    public MeterReadingsController(IMeterReadingService service)
    {
        _service = service;
    }

    // lấy danh sách chỉ số điện nước của các căn hộ trong tòa nhà 
    [HttpGet("recording-sheet")]
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


    // lưu số điện nước 
    [HttpPost("record")]
    public async Task<ActionResult<ApiResponse<MeterReadingDto>>> RecordReading(
        [FromBody] RecordMeterReadingRequest request,
        [FromQuery] string staffId)
    {
        try
        {
            string billingPeriod = DateTime.Now.ToString("yyyy-MM");
            var result = await _service.RecordMeterReadingAsync(request, staffId, billingPeriod);

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

    // lấy tiến độ ghi chỉ số cho một tòa nhà trong một kỳ hóa đơn
    [HttpGet("progress/{buildingCode}")]
    public async Task<ActionResult<ApiResponse<RecordingProgressDto>>> GetProgress(
        string buildingCode,
        [FromQuery] string? billingPeriod = null)
    {
        try
        {
            billingPeriod ??= DateTime.Now.ToString("yyyy-MM");

            // validate billing period format
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


    // tạo hóa đơn cho tất cả căn hộ trong tòa nhà 
    [HttpPost("generate-invoices/{buildingCode}")]
    public async Task<ActionResult<ApiResponse<int>>> GenerateInvoices(
        string buildingCode)
    {
        try
        {
            string billingPeriod = DateTime.Now.ToString("yyyy-MM");

            // kiểm tra tiến độ ghi chỉ số - phải đạt 100% cho tất cả loại đồng hồ trước khi tạo hóa đơn
            var progress = await _service.GetRecordingProgressAsync(buildingCode, billingPeriod);
            
            // kiểm tra xem tất cả loại đồng hồ đã hoàn thành chưa
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

            // sử dụng BuildingId từ kết quả tiến độ ghi chỉ số
            var count = await _service.GenerateMonthlyInvoicesAsync(progress.BuildingId, billingPeriod);
            
            if (count == 0)
            {
                // nếu không có hóa đơn nào được tạo, có thể là vì tất cả chỉ số đã được ghi
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

    // lấy tất cả chỉ số điện nước đã ghi cho một tòa nhà trong một kỳ hóa đơn
    [HttpGet("recorded-readings")]
    public async Task<ActionResult<ApiResponse<List<MeterReadingDto>>>> GetRecordedReadings(
        [FromQuery] string buildingCode,
        [FromQuery] string billingPeriod)
    {
        try
        {
            // validate billing period format
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

    // lấy lịch sử ghi chỉ số cho một căn hộ
    [HttpGet("history/{apartmentId}")]
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

