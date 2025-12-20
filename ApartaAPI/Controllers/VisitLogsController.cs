using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitLogsController : ControllerBase
    {
        private readonly IVisitLogService _service;

        public VisitLogsController(IVisitLogService service)
        {
            _service = service;
        }

        // ======================================================
        // 1. TẠO MỚI (Từ VisitorsController gộp sang)
        // POST: api/VisitLogs/fast-checkin
        // ======================================================
        [HttpPost("fast-checkin")]
        public async Task<ActionResult<VisitorDto>> CreateVisit([FromBody] VisitorCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Gọi hàm CreateVisitAsync đã gộp trong IVisitLogService
                var createdVisitor = await _service.CreateVisitAsync(dto);
                return Ok(createdVisitor);
            }
            catch (ValidationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ nội bộ: {ex.Message}");
            }
        }

        // ======================================================
        // 2. LẤY KHÁCH CŨ (Từ VisitorsController gộp sang)
        // GET: api/VisitLogs/recent
        // ======================================================
        [HttpGet("recent")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<VisitorDto>>> GetRecentVisitors()
        {
            // Lấy ApartmentId từ Token của User đang đăng nhập
            var apartmentId = User.FindFirst("apartment_id")?.Value;

            if (string.IsNullOrEmpty(apartmentId))
            {
                return BadRequest(new { message = "Tài khoản này không gắn liền với căn hộ nào." });
            }

            var visitors = await _service.GetRecentVisitorsAsync(apartmentId);
            return Ok(visitors);
        }

        // ======================================================
        // 3. DÀNH CHO STAFF (Xem danh sách quản lý)
        // GET: api/VisitLogs/all
        // ======================================================
        [HttpGet("all")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse<PagedList<VisitLogStaffViewDto>>>> GetVisitLogsForStaff(
            [FromQuery] VisitorQueryParameters queryParams)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var pagedData = await _service.GetStaffViewLogsAsync(queryParams, userId);

            if (pagedData.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData, ApiResponse.SM01_NO_RESULTS));
            }
            return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData));
        }

        // ======================================================
        // 4. DÀNH CHO RESIDENT (Xem lịch sử của chính mình)
        // GET: api/VisitLogs/my-history
        // ======================================================
        [HttpGet("my-history")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedList<VisitLogStaffViewDto>>>> GetMyVisitHistory(
            [FromQuery] VisitorQueryParameters queryParams)
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var pagedData = await _service.GetResidentHistoryAsync(queryParams, userId);

            if (pagedData.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData, ApiResponse.SM01_NO_RESULTS));
            }
            return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData));
        }

        // ======================================================
        // 5. CẬP NHẬT TRẠNG THÁI (Check-in/Out)
        // ======================================================
        [HttpPut("{id}/checkin")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse>> CheckInVisit(string id)
        {
            var success = await _service.CheckInAsync(id);
            //if (!success) return BadRequest(ApiResponse.Fail("Thao tác thất bại hoặc sai trạng thái."));
            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }

        [HttpPut("{id}/checkout")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse>> CheckOutVisit(string id)
        {
            var success = await _service.CheckOutAsync(id);
            //if (!success) return BadRequest(ApiResponse.Fail("Thao tác thất bại hoặc sai trạng thái."));
            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }

        // ======================================================
        // 6. XÓA VÀ CẬP NHẬT THÔNG TIN
        // ======================================================
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteVisitLog(string id)
        {
            var success = await _service.DeleteLogAsync(id);
            if (!success) return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
            return Ok(ApiResponse.Success(ApiResponse.SM05_DELETION_SUCCESS));
        }

        [HttpPut("{id}/info")]
        public async Task<ActionResult<ApiResponse>> UpdateVisitLog(string id, [FromBody] VisitLogUpdateDto dto)
        {
            var success = await _service.UpdateLogAsync(id, dto);
            //if (!success) return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }
    }
}