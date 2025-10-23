using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitorsController : ControllerBase 
    {
        private readonly IVisitorService _service; 

        public VisitorsController(IVisitorService service) 
        {
            _service = service;
        }

        // GET: api/Visitors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VisitorDto>>> GetVisitors()
        {
            var visitors = await _service.GetAllAsync();
            return Ok(visitors);
        }

        // GET: api/Visitors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VisitorDto>> GetVisitor(string id)
        {
            var visitor = await _service.GetByIdAsync(id);
            if (visitor == null) return NotFound();
            return Ok(visitor);
        }

        // POST: api/Visitors
        [HttpPost]
        public async Task<ActionResult<VisitorDto>> PostVisitor([FromBody] VisitorCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            // Thay đổi Id
            return CreatedAtAction(nameof(GetVisitor), new { id = created.VisitorId }, created);
        }

        // PUT: api/Visitors/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVisitor(string id, [FromBody] VisitorUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return NoContent();
        }

        // DELETE: api/Visitors/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVisitor(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}