using ApartaAPI.DTOs.Assets;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
        {
            var assets = await _service.GetAllAsync();
            return Ok(assets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AssetDto>> GetAsset(string id)
        {
            var asset = await _service.GetByIdAsync(id);
            if (asset == null) return NotFound();
            return Ok(asset);
        }

        [HttpPost]
        public async Task<ActionResult<AssetDto>> PostAsset([FromBody] AssetCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetAsset), new { id = created.AssetId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsset(string id, [FromBody] AssetUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return Ok(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok(); 
        }
    }
}
