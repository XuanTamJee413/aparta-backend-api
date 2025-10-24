using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApartmentMembersController : ControllerBase
    {
        private readonly IApartmentMemberService _service;

        public ApartmentMembersController(IApartmentMemberService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApartmentMemberDto>>>> GetApartmentMembers(
            [FromQuery] ApartmentMemberQueryParameters query)
        {
            var apiResponse = await _service.GetAllAsync(query);
            return Ok(apiResponse); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApartmentMemberDto>> GetApartmentMember(string id)
        {
            var member = await _service.GetByIdAsync(id);
            if (member == null) return NotFound();
            return Ok(member);
        }

        [HttpPost]
        public async Task<ActionResult<ApartmentMemberDto>> PostApartmentMember([FromBody] ApartmentMemberCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            
            return CreatedAtAction(nameof(GetApartmentMember), new { id = created.ApartmentMemberId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutApartmentMember(string id, [FromBody] ApartmentMemberUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return Ok(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApartmentMember(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(); 
        }
    }
}