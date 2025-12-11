using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // phuong thuc da join visitor de lay visitor id, visitor name, join apartment de lay apartment code
        // GET: api/VisitLogs/all
        [HttpGet("all")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse<PagedList<VisitLogStaffViewDto>>>> GetVisitLogsForStaff(
            [FromQuery] VisitorQueryParameters queryParams)
        {
            var pagedData = await _service.GetStaffViewLogsAsync(queryParams);

            if (pagedData.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData, ApiResponse.SM01_NO_RESULTS));
            }

            return Ok(ApiResponse<PagedList<VisitLogStaffViewDto>>.Success(pagedData));
        }

        [HttpPut("{id}/checkin")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse>> CheckInVisit(string id)
        {
            var success = await _service.CheckInAsync(id);

            //if (!success)
            //{
            //    return Ok(ApiResponse.Fail("Không tìm thấy lượt thăm hoặc lượt thăm không ở trạng thái 'Pending'."));
            //}

            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }

        [HttpPut("{id}/checkout")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse>> CheckOutVisit(string id)
        {
            var success = await _service.CheckOutAsync(id);

            //if (!success)
            //{
            //    return Ok(ApiResponse.Fail("Không tìm thấy lượt thăm hoặc lượt thăm chưa check-in."));
            //}

            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }

        // DELETE: api/VisitLogs/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanReadVisitor")] // Hoặc Policy phù hợp
        public async Task<ActionResult<ApiResponse>> DeleteVisitLog(string id)
        {
            var success = await _service.DeleteLogAsync(id);
            if (!success) return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
            return Ok(ApiResponse.Success(ApiResponse.SM05_DELETION_SUCCESS));
        }

        // PUT: api/VisitLogs/{id}/info
        [HttpPut("{id}/info")]
        [Authorize(Policy = "CanReadVisitor")]
        public async Task<ActionResult<ApiResponse>> UpdateVisitLog(string id, [FromBody] VisitLogUpdateDto dto)
        {
            var success = await _service.UpdateLogAsync(id, dto);
            if (!success) return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
            return Ok(ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS));
        }
    }
}