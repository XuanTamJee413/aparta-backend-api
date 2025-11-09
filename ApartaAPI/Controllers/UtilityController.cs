using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Utilities;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

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

		// GET: api/Utility
		[HttpGet]
		[Authorize(Policy = "CanReadUtility")]
		[ProducesResponseType(typeof(PagedList<UtilityDto>), StatusCodes.Status200OK)] 
		public async Task<ActionResult<PagedList<UtilityDto>>> GetUtilities(
			[FromQuery] ServiceQueryParameters parameters) 
		{
			var utilities = await _utilityService.GetAllUtilitiesAsync(parameters);
			return Ok(utilities);
		}

		// GET: api/Utility/{id}
		[HttpGet("{id}")]
        [Authorize(Policy = "CanReadUtility")]
        [ProducesResponseType(typeof(UtilityDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<UtilityDto>> GetUtility(string id)
		{
			var utilityDto = await _utilityService.GetUtilityByIdAsync(id);

			if (utilityDto == null)
			{
				return NotFound();
			}

			return Ok(utilityDto);
		}

		// POST: api/Utility
		[HttpPost]
		[Authorize(Policy = "CanCreateUtility")]
		[ProducesResponseType(typeof(UtilityDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status409Conflict)] 
		public async Task<ActionResult<UtilityDto>> PostUtility([FromBody] UtilityCreateDto utilityCreateDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				var createdUtilityDto = await _utilityService.AddUtilityAsync(utilityCreateDto);

				return CreatedAtAction(
					nameof(GetUtility),
					new { id = createdUtilityDto.UtilityId },
					createdUtilityDto
				);
			}
			catch (ArgumentException ex) 
			{
				return BadRequest(new { message = ex.Message }); 
			}
			catch (InvalidOperationException ex) 
			{
				return Conflict(new { message = ex.Message }); 
			}
		}

		// PUT: api/Utility/{id}
		[HttpPut("{id}")]
		[Authorize(Policy = "CanUpdateUtility")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)] 
		public async Task<IActionResult> PutUtility(string id, [FromBody] UtilityUpdateDto utilityUpdateDto)
		{
			try
			{
				var updatedUtilityDto = await _utilityService.UpdateUtilityAsync(id, utilityUpdateDto);

				if (updatedUtilityDto == null)
				{
					return NotFound();
				}

				return NoContent();
			}
			catch (ArgumentException ex) 
			{
				return BadRequest(new { message = ex.Message }); 
			}
			catch (InvalidOperationException ex) 
			{
				return Conflict(new { message = ex.Message });
			}
		}

		// DELETE: api/Utility/{id}
		[HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteUtility")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DeleteUtility(string id)
		{
			var result = await _utilityService.DeleteUtilityAsync(id);

			if (!result)
			{
				return NotFound();
			}

			return NoContent();
		}
	}
}