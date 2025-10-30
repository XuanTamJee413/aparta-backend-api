using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Vehicles;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _service;

        public VehiclesController(IVehicleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VehicleDto>>>> GetVehicles([FromQuery] VehicleQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDto>> GetVehicle(string id)
        {
            var vehicle = await _service.GetByIdAsync(id);
            if (vehicle == null) return NotFound();
            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<ActionResult<VehicleDto>> PostVehicle([FromBody] VehicleCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetVehicle), new { id = created.VehicleId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicle(string id, [FromBody] VehicleUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return Ok(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(); 
        }
    }
}
