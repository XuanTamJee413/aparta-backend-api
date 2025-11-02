using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization; // Uncomment if authorization is needed

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildingsController : ControllerBase
    {
        private readonly IBuildingService _service;

        public BuildingsController(IBuildingService service)
        {
            _service = service;
        }

        // GET: api/Buildings
        [HttpGet]
        [Authorize(Policy = "CanReadBuilding")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<BuildingDto>>>> GetBuildings(
            [FromQuery] BuildingQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            return Ok(response);
        }

        // GET: api/Buildings/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadBuilding")]
        public async Task<ActionResult<ApiResponse<BuildingDto>>> GetBuilding(string id)
        {
            var response = await _service.GetByIdAsync(id);

            if (!response.Succeeded)
            {
                // Assuming SM01 maps to NotFound
                return NotFound(response);
            }

            return Ok(response);
        }

        // POST: api/Buildings
        [HttpPost]
        [Authorize(Policy = "CanCreateBuilding")]
        public async Task<ActionResult<ApiResponse<BuildingDto>>> PostBuilding([FromBody] BuildingCreateDto request)
        {
            if (!ModelState.IsValid)
            {
                // Create a generic fail response if model state is invalid
                return BadRequest(ApiResponse<BuildingDto>.Fail("Invalid input data."));
            }

            var response = await _service.CreateAsync(request);

            if (!response.Succeeded)
            {
                // Assuming SM02 or SM16 maps to BadRequest
                return BadRequest(response);
            }

            // Return 201 Created with location header and response body
            return CreatedAtAction(
                nameof(GetBuilding),
                new { id = response.Data!.BuildingId },
                response
            );
        }

        // PUT: api/Buildings/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateBuilding")]
        public async Task<ActionResult<ApiResponse>> PutBuilding(string id, [FromBody] BuildingUpdateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Invalid input data."));
            }

            var response = await _service.UpdateAsync(id, request);

            if (!response.Succeeded)
            {
                if (response.Message == "SM01") // Not found
                {
                    return NotFound(response);
                }
                // Assuming other failures like SM02 are Bad Request
                return BadRequest(response);
            }

            // Return OK with the success message (SM03)
            return Ok(response);
        }

        // No DELETE endpoint as per requirement (deactivation handled via PUT)
    }
}