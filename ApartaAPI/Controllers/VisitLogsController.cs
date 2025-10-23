using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.VisitLogs;
using ApartaAPI.Services.Interfaces;

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

        // GET: api/VisitLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VisitLogDto>>> GetVisitLogs()
        {
            var logs = await _service.GetAllAsync();
            return Ok(logs);
        }

        // GET: api/VisitLogs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VisitLogDto>> GetVisitLog(string id)
        {
            var log = await _service.GetByIdAsync(id);
            if (log == null) return NotFound();
            return Ok(log);
        }

        // POST: api/VisitLogs
        [HttpPost]
        public async Task<ActionResult<VisitLogDto>> PostVisitLog([FromBody] VisitLogCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetVisitLog), new { id = created.Id }, created);
        }

        // PUT: api/VisitLogs/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVisitLog(string id, [FromBody] VisitLogUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return NoContent();
        }

        // DELETE: api/VisitLogs/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVisitLog(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // GET: api/VisitLogs/apartment/{apartmentId}
        [HttpGet("apartment/{apartmentId}")]
        public async Task<ActionResult<IEnumerable<VisitLogHistoryDto>>> GetVisitLogsForApartment(string apartmentId)
        {
            var logs = await _service.GetByApartmentIdAsync(apartmentId);
            return Ok(logs);
        }
    }
}