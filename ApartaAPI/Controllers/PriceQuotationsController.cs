using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.Utils.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Cho phép tất cả role đã authenticated

    public class PriceQuotationsController : ControllerBase
    {
        private readonly IPriceQuotationService _priceQuotationService;

        public PriceQuotationsController(IPriceQuotationService priceQuotationService)
        {
            _priceQuotationService = priceQuotationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPriceQuotations()
        {
            var priceQuotations = await _priceQuotationService.GetPriceQuotationsAsync();
            return Ok(priceQuotations);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePriceQuotation([FromBody] PriceQuotationCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdPriceQuotation = await _priceQuotationService.CreatePriceQuotationAsync(createDto);

            if (createdPriceQuotation == null)
            {
                return NotFound($"Building with ID '{createDto.BuildingId}' not found.");
            }

            return Created(string.Empty, createdPriceQuotation);

        }

        [HttpGet("building/{buildingId}")]
        public async Task<IActionResult> GetPriceQuotationsByBuilding(string buildingId)
        {
            var priceQuotations = await _priceQuotationService.GetPriceQuotationsByBuildingIdAsync(buildingId);

            if (priceQuotations == null)
            {
                return NotFound($"Building with ID '{buildingId}' not found.");
            }

            return Ok(priceQuotations);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PriceQuotationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPriceQuotationById(string id)
        {
            var priceQuotation = await _priceQuotationService.GetPriceQuotationByIdAsync(id);

            if (priceQuotation == null)
            {
                return NotFound($"PriceQuotation with ID '{id}' not found.");
            }

            return Ok(priceQuotation);
        }

        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse<PagedList<PriceQuotationDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PagedList<PriceQuotationDto>>>> GetPriceQuotationsPaginated(
                            [FromQuery] PriceQuotationQueryParameters queryParams)
        {
            // 1. Lấy UserId và Role từ Token
            var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse.Fail("Không tìm thấy thông tin người dùng."));

            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            // 2. Gọi service với userId và isAdmin flag
            var pagedData = await _priceQuotationService.GetPriceQuotationsPaginatedAsync(queryParams, userId, isAdmin);

            if (pagedData.TotalCount == 0)
            {
                return Ok(ApiResponse<PagedList<PriceQuotationDto>>.Success(pagedData, ApiResponse.SM01_NO_RESULTS));
            }
            return Ok(ApiResponse<PagedList<PriceQuotationDto>>.Success(pagedData));
        }

        [HttpGet("details/{id}")]
        [ProducesResponseType(typeof(ApiResponse<PriceQuotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PriceQuotationDto>>> GetPriceQuotationDetailsById(string id)
        {
            var priceQuotation = await _priceQuotationService.GetPriceQuotationByIdAsync(id);
            if (priceQuotation == null)
            {
                return NotFound(ApiResponse<PriceQuotationDto>.Fail(ApiResponse.SM01_NO_RESULTS));
            }
            return Ok(ApiResponse<PriceQuotationDto>.Success(priceQuotation));
        }

        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<PriceQuotationDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<PriceQuotationDto>>> CreatePriceQuotationV2(
            [FromBody] PriceQuotationCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Dữ liệu đầu vào không hợp lệ."));
            }

            try
            {
                var createdPriceQuotation = await _priceQuotationService.CreatePriceQuotationAsync(createDto);
                if (createdPriceQuotation == null)
                {
                    return NotFound(ApiResponse.Fail($"Building with ID '{createDto.BuildingId}' not found."));
                }

                return CreatedAtAction(
                    nameof(GetPriceQuotationById),
                    new { id = createdPriceQuotation.PriceQuotationId },
                    ApiResponse<PriceQuotationDto>.SuccessWithCode(createdPriceQuotation, ApiResponse.SM04_CREATE_SUCCESS, "Price Quotation")
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> UpdatePriceQuotation(string id, [FromBody] PriceQuotationCreateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("Dữ liệu đầu vào không hợp lệ."));
            }

            try
            {
                var success = await _priceQuotationService.UpdateAsync(id, updateDto);
                //if (!success)
                //{
                //    return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
                //}
                return Ok(ApiResponse.SuccessWithCode(ApiResponse.SM03_UPDATE_SUCCESS));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse>> DeletePriceQuotation(string id)
        {
            var success = await _priceQuotationService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS));
            }
            return Ok(ApiResponse.SuccessWithCode(ApiResponse.SM05_DELETION_SUCCESS, "Price Quotation"));
        }

        [HttpGet("calculation-methods")]
        [ProducesResponseType(typeof(ApiResponse<List<EnumOptionDto>>), StatusCodes.Status200OK)]
        public IActionResult GetCalculationMethods()
        {
            try
            {
                var methods = EnumHelper.GetCalculationMethodOptions();
                return Ok(ApiResponse<List<EnumOptionDto>>.Success(methods));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.Fail(ex.Message));
            }
        }
    }
}
