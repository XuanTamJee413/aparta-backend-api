using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Utilities;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Để dùng ClaimTypes
using System.Threading.Tasks;
using System;

namespace ApartaAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UtilityController : ControllerBase
	{
		private readonly IUtilityService _utilityService;

		public UtilityController(IUtilityService utilityService)
		{
			_utilityService = utilityService;
		}

		// Helper để lấy UserId
		private string GetCurrentUserId()
		{
			var userId = User.FindFirstValue("id");

			// 2. Nếu không thấy, tìm claim chuẩn NameIdentifier (http://schemas.../nameidentifier)
			if (string.IsNullOrEmpty(userId))
			{
				userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			}

			// 3. Nếu vẫn không thấy, thử tìm "sub" (Subject - chuẩn JWT)
			if (string.IsNullOrEmpty(userId))
			{
				userId = User.FindFirstValue("sub");
			}

			// Nếu tất cả đều null thì mới báo lỗi
			return userId ?? throw new UnauthorizedAccessException("User ID not found in token.");
		}

		// GET: api/Utility
		[HttpGet]
		[Authorize(Policy = "CanReadUtility")]
		public async Task<ActionResult<PagedList<UtilityDto>>> GetUtilities([FromQuery] ServiceQueryParameters parameters)
		{
			var userId = GetCurrentUserId();
			var utilities = await _utilityService.GetAllUtilitiesAsync(parameters, userId);
			return Ok(utilities);
		}

		// GET: api/Utility/{id}
		[HttpGet("{id}")]
		[Authorize(Policy = "CanReadUtility")]
		public async Task<ActionResult<UtilityDto>> GetUtility(string id)
		{
			var userId = GetCurrentUserId();
			var utilityDto = await _utilityService.GetUtilityByIdAsync(id, userId);

			if (utilityDto == null)
			{
				return NotFound();
			}

			return Ok(utilityDto);
		}

		// POST: api/Utility
		[HttpPost]
		[Authorize(Policy = "CanCreateUtility")]
		public async Task<ActionResult<UtilityDto>> PostUtility([FromBody] UtilityCreateDto utilityCreateDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			try
			{
				var userId = GetCurrentUserId();
				var createdUtilityDto = await _utilityService.AddUtilityAsync(utilityCreateDto, userId);

				return CreatedAtAction(nameof(GetUtility), new { id = createdUtilityDto.UtilityId }, createdUtilityDto);
			}
			catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
			catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
			catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); } // 403 Forbidden
		}

		// PUT: api/Utility/{id}
		[HttpPut("{id}")]
		[Authorize(Policy = "CanUpdateUtility")]
		public async Task<IActionResult> PutUtility(string id, [FromBody] UtilityUpdateDto utilityUpdateDto)
		{
			try
			{
				var userId = GetCurrentUserId();
				var updatedUtilityDto = await _utilityService.UpdateUtilityAsync(id, utilityUpdateDto, userId);

				if (updatedUtilityDto == null) return NotFound();

				return NoContent();
			}
			catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
			catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
			catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
		}

		// DELETE: api/Utility/{id}
		[HttpDelete("{id}")]
		[Authorize(Policy = "CanDeleteUtility")]
		public async Task<IActionResult> DeleteUtility(string id)
		{
			try
			{
				var userId = GetCurrentUserId();
				var result = await _utilityService.DeleteUtilityAsync(id, userId);

				if (!result) return NotFound();

				return NoContent();
			}
			catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
		}

		//GET: api/Utility/resident(Dành cho Cư dân)

		[HttpGet("resident")]
		[Authorize] 
		public async Task<ActionResult<PagedList<UtilityDto>>> GetUtilitiesForResident([FromQuery] ServiceQueryParameters parameters)
		{
			var userId = GetCurrentUserId();
			var utilities = await _utilityService.GetUtilitiesForResidentAsync(parameters, userId);
			return Ok(utilities);
		}
	}
}