using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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

        [HttpGet("my-buildings")]
        [Authorize(Policy = "CanReadMember")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ApartmentMemberDto>>>> GetApartmentMembersByMyBuildings( [FromQuery] ApartmentMemberQueryParameters query)
        {
            var userId =
                User.FindFirst("id")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(
                    ApiResponse.Fail("AUTH01", "Không xác định được tài khoản đăng nhập.")
                );
            }

            var apiResponse = await _service.GetByUserBuildingsAsync(userId, query);
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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApartmentMemberDto>> PostApartmentMember(
            [FromForm] ApartmentMemberCreateDto request,
            IFormFile? faceImageFile,
            CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateAsync(request, faceImageFile, cancellationToken);

                return CreatedAtAction(
                    nameof(GetApartmentMember),
                    new { id = created.ApartmentMemberId },
                    created
                );
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateMember")]
        public async Task<IActionResult> PutApartmentMember(string id, [FromBody] ApartmentMemberUpdateDto request)
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

        [HttpPut("{id}/avatar")]
        [Authorize(Policy = "CanUpdateMember")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateMemberAvatar(
            string id,
            [FromForm] UpdateMemberAvatarRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid ||
                request.FaceImageFile == null ||
                request.FaceImageFile.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var response = await _service.UpdateFaceImageAsync(id, request.FaceImageFile, cancellationToken);

            if (!response.Succeeded)
            {
                if (response.Message == "Không tìm thấy thành viên hộ khẩu.")
                    return NotFound(response);

                if (response.Message == ApiResponse.SM40_SYSTEM_ERROR)
                    return StatusCode(500, response);

                return BadRequest(response);
            }

            return Ok(response);
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
