using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize(Policy = "CanReadMember")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApartmentMemberDto>>>> GetApartmentMembers(
            [FromQuery] ApartmentMemberQueryParameters query)
        {
            var apiResponse = await _service.GetAllAsync(query);
            return Ok(apiResponse); 
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadMember")]
        public async Task<ActionResult<ApartmentMemberDto>> GetApartmentMember(string id)
        {
            var member = await _service.GetByIdAsync(id);
            if (member == null) return NotFound();
            return Ok(member);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateMember")]
        public async Task<ActionResult<ApartmentMemberDto>> PostApartmentMember([FromBody] ApartmentMemberCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            
            return CreatedAtAction(nameof(GetApartmentMember), new { id = created.ApartmentMemberId }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateMember")]
        public async Task<IActionResult> PutApartmentMember(string id, [FromBody] ApartmentMemberUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return Ok(); 
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteMember")]
        public async Task<IActionResult> DeleteApartmentMember(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(); 
        }
    }
}