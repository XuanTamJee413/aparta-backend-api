using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Exceptions;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _service;
        private readonly IContractPdfService _pdfService;
        public ContractsController(IContractService service, IContractPdfService pdfService)
        {
            _service = service;
            _pdfService = pdfService;
        }

        [HttpGet]
        [Authorize(Policy = "CanReadContract")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContractDto>>>> GetContracts([FromQuery] ContractQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "CanReadContract")]
        public async Task<ActionResult<ContractDto>> GetContract(string id)
        {
            var contract = await _service.GetByIdAsync(id);
            if (contract == null) return NotFound();
            return Ok(contract);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateContract")]
        public async Task<ActionResult<ApiResponse<ContractDto>>> PostContract([FromBody] CreateContractRequestDto request)
        {
            // Gọi Service, nhận về ApiResponse (chứa data hoặc lỗi)
            var response = await _service.CreateAsync(request);

            if (response.Succeeded && response.Data is not null)
            {
                // Thành công: Trả về 201 Created kèm dữ liệu
                return CreatedAtAction(nameof(GetContract), new { id = response.Data.ContractId }, response);
            }

            // Thất bại: Kiểm tra mã lỗi để trả về status code phù hợp
            // Ví dụ: Lỗi không tìm thấy -> 404, Lỗi nghiệp vụ -> 400
            if (response.Message == ApiResponse.GetMessageFromCode(ApiResponse.SM58_APARTMENT_NOT_FOUND))
            {
                return NotFound(response);
            }

            // Mặc định các lỗi khác là 400 Bad Request
            return BadRequest(response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateContract")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutContract(string id, [FromForm] ContractUpdateDto request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);
                return Ok(new { message = "Cập nhật hợp đồng thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình cập nhật hợp đồng." });
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteContract")]
        public async Task<IActionResult> DeleteContract(string id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted) return NotFound();

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DownloadContractPdf(string id)
        {
            var contract = await _service.GetByIdAsync(id);
            if (contract == null)
                return NotFound(new { message = "Không tìm thấy hợp đồng." });

            var pdfBytes = _pdfService.GenerateContractPdf(contract);
            var fileName = $"hop-dong-{contract.ApartmentCode ?? contract.ApartmentId}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("my-buildings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContractDto>>>> GetContractsByMyBuildings([FromQuery] ContractQueryParameters query)
        {
            var userId = User.FindFirst("id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("AUTH01", "Không xác định được tài khoản đăng nhập."));
            }

            var response = await _service.GetByUserBuildingsAsync(userId, query);
            return Ok(response);
        }

    }
}