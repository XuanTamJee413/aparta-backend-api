using ApartaAPI.DTOs.Utilities;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; 

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
		[ProducesResponseType(typeof(IEnumerable<UtilityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<UtilityDto>>> GetUtilities()
		{
			var utilities = await _utilityService.GetAllUtilitiesAsync();
			return Ok(utilities);
		}

		// GET: api/Utility/{id}
		[HttpGet("{id}")]
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
		[ProducesResponseType(typeof(UtilityDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<UtilityDto>> PostUtility([FromBody] UtilityCreateDto utilityCreateDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var createdUtilityDto = await _utilityService.AddUtilityAsync(utilityCreateDto);

			return CreatedAtAction(
				nameof(GetUtility),
				new { id = createdUtilityDto.UtilityId },
				createdUtilityDto
			);
		}

		// PUT: api/Utility/{id}
		[HttpPut("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> PutUtility(string id, [FromBody] UtilityUpdateDto utilityUpdateDto)
		{
			var updatedUtilityDto = await _utilityService.UpdateUtilityAsync(id, utilityUpdateDto);

			if (updatedUtilityDto == null)
			{
				return NotFound();
			}

			return NoContent();
		}

		// DELETE: api/Utility/{id}
		[HttpDelete("{id}")]
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