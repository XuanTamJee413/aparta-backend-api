using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        /// <summary>
        /// Lấy danh sách tất cả đơn giá (fees) của một tòa nhà cụ thể.
        /// </summary>
        /// <param name="buildingId">ID của tòa nhà cần xem.</param>
        /// <response code="200">Trả về danh sách các đơn giá (có thể rỗng).</response>
        /// <response code="404">Không tìm thấy tòa nhà (Building) với ID đã cung cấp.</response>
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

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn giá (fee) theo ID.
        /// </summary>
        /// <param name="id">ID của đơn giá cần tìm.</param>
        /// <response code="200">Trả về thông tin chi tiết của đơn giá.</response>
        /// <response code="404">Không tìm thấy đơn giá với ID đã cung cấp.</response>
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
    }
}
