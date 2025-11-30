using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Assets;
using ApartaAPI.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartaAPI.DTOs.Common; 
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _service;

        public AssetsController(IAssetService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = "CanReadAsset")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AssetDto>>>> GetAssets(
            [FromQuery] AssetQueryParameters query) 
        {
            var response = await _service.GetAllAsync(query); 
            return Ok(response); 
        }
        [HttpGet("my-buildings")]
        [Authorize(Policy = "CanReadAsset")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AssetDto>>>> GetAssetsByMyBuildings( [FromQuery] AssetQueryParameters query)
        {
            var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("AUTH01", "Không xác định được tài khoản đăng nhập."));
            }

            var response = await _service.GetByUserBuildingsAsync(userId, query);
            return Ok(response);
        }


        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadAsset")]
        public async Task<ActionResult<AssetDto>> GetAsset(string id)
        {
            var asset = await _service.GetByIdAsync(id);
            if (asset == null) return NotFound();
            return Ok(asset);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateAsset")]
        public async Task<ActionResult<AssetDto>> PostAsset([FromBody] AssetCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetAsset), new { id = created.AssetId }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateAsset")]
        public async Task<IActionResult> PutAsset(string id, [FromBody] AssetUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            //if (!updated) return NotFound(); 
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteAsset")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok();
        }
    }
}