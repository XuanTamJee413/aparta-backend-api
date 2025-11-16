using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _service;

        public ContractsController(IContractService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContractDto>>>> GetContracts([FromQuery] ContractQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContractDto>> GetContract(string id)
        {
            var contract = await _service.GetByIdAsync(id);
            if (contract == null) return NotFound();
            return Ok(contract);
        }

        [HttpPost]
        public async Task<ActionResult<ContractDto>> PostContract([FromBody] ContractCreateDto request)
        {
            try
            {
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetContract), new { id = created.ContractId }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình tạo hợp đồng.", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutContract(string id, [FromBody] ContractUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return Ok();
        }
    }
}