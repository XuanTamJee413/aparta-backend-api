using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Apartments;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApartmentsController : ControllerBase
    {
        private readonly IApartmentService _service;

        public ApartmentsController(IApartmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApartmentDto>>>> GetApartments([FromQuery] ApartmentQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApartmentDto>> GetApartment(string id)
        {
            var apartment = await _service.GetByIdAsync(id);
            if (apartment == null) return NotFound();
            return Ok(apartment);
        }

        [HttpPost]
        public async Task<ActionResult<ApartmentDto>> PostApartment([FromBody] ApartmentCreateDto request)
        {
            try
            {
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetApartment), new { id = created.ApartmentId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<ApartmentDto>>> PostApartmentsBulk([FromBody] ApartmentBulkCreateDto request)
        {
            try
            {
                var created = await _service.CreateBulkAsync(request);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutApartment(string id, [FromBody] ApartmentUpdateDto request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApartment(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            return Ok();
        }
    }
}
