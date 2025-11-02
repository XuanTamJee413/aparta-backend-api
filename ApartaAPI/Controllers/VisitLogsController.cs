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
        public async Task<ActionResult<IEnumerable<VisitLogStaffViewDto>>> GetVisitLogsForStaff()
        {
            var logs = await _service.GetStaffViewLogsAsync();
            return Ok(logs);
        }

        [HttpPut("{id}/checkin")]
        [Authorize(Policy = "CanCheckInVisitor")]
        public async Task<IActionResult> CheckInVisit(string id)
        {
            var success = await _service.CheckInAsync(id);

            if (!success)
            {
                return NotFound("Không tìm thấy lượt thăm hoặc lượt thăm không ở trạng thái 'Pending'.");
            }

            return Ok();
        }

        [HttpPut("{id}/checkout")]
        [Authorize(Policy = "CanCheckOutVisitor")]
        public async Task<IActionResult> CheckOutVisit(string id)
        {
            var success = await _service.CheckOutAsync(id);

            if (!success)
            {
                return NotFound("Không tìm thấy lượt thăm hoặc lượt thăm chưa check-in.");
            }

            return Ok();
        }
    }
}