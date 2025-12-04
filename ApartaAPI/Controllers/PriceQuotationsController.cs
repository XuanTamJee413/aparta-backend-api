using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.Utils.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ManagerAccess")]

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

        /// <summary>
        /// Tạo một đơn giá(fee) mới cho một tòa nhà.
        /// </summary>
        /// <remarks>
        /// Endpoint này cho phép tạo một cấu hình đơn giá mới, ví dụ: "Phí quản lý", "Tiền điện".
        /// 
        /// **Lưu ý về `calculationMethod` và `note`:**
        /// - Nếu dùng `calculationMethod` là "TIERED" (lũy tiến), trường `note` BẮT BUỘC phải chứa một 
        ///   chuỗi JSON hợp lệ định nghĩa các bậc giá. unitPrice phải điền mức giá thấp nhất (hoặc trung bình) 
        ///   phòng TH json không hợp lệ sẽ sử dụng unitPrice để tính
        /// - Nếu dùng các phương thức khác(như "PER_AREA", "FIXED_RATE"), trường `unitPrice` sẽ được sử dụng
        ///   và trường `note` có thể để trống.
        /// 
        /// ---
        /// ### Ví dụ JSON cho `TIERED`:
        /// ```json
        /// {
        ///   "buildingId": "1",
        ///   "feeType": "Tiền điện sinh hoạt",
        ///   "calculationMethod": "TIERED",
        ///   "unitPrice": 1800,
        ///   "unit": "VND/kWh",
        ///   "note": "[{\"fromValue\":0,\"toValue\":100,\"unitPrice\":1800},{\"fromValue\":101,\"toValue\":200,\"unitPrice\":2500},{\"fromValue\":201,\"toValue\":400,\"unitPrice\":3000},{\"fromValue\":401,\"toValue\":null,\"unitPrice\":3500}]"
        /// }
        /// ```
        /// </remarks>
        /// <param name="createDto">Đối tượng chứa thông tin của đơn giá cần tạo.</param>
        /// <response code="201">Tạo thành công. Trả về đối tượng đơn giá vừa được tạo.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ (ví dụ: thiếu trường bắt buộc, `unitPrice` âm).</response>
        /// <response code="404">Không tìm thấy `buildingId` được chỉ định trong hệ thống.</response>
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
            var pagedData = await _priceQuotationService.GetPriceQuotationsPaginatedAsync(queryParams);

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
