using ApartaAPI.DTOs.Buildings;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Subscriptions;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")] // (UC 2.1.1 - Ex 1E) Chỉ Admin
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _service;

        public SubscriptionsController(ISubscriptionService service)
        {
            _service = service;
        }

        /// <summary>
        /// (UC 2.1.1) Lấy danh sách Subscriptions (Main hoặc Draft)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<SubscriptionDto>>>> GetSubscriptions(
            [FromQuery] SubscriptionQueryParameters query)
        {
            // FE sẽ truyền query.Status="Draft" để lấy danh sách nháp
            // FE sẽ không truyền query.Status hoặc truyền status khác ("Active", "Expired") để lấy danh sách chính
            var response = await _service.GetAllAsync(query);
            return Ok(response); // Luôn trả về 200 OK, FE kiểm tra Succeeded và Message (SM01)
        }

        /// <summary>
        /// Lấy chi tiết một Subscription (Draft hoặc Approved) bằng ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetSubscription(string id)
        {
            var response = await _service.GetByIdAsync(id);
            if (!response.Succeeded)
            {
                return NotFound(response); // 404 nếu SM01
            }
            return Ok(response);
        }

        /// <summary>
        /// (UC 2.1.2) Tạo mới một bản ghi gia hạn (Lưu Nháp hoặc Duyệt)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> PostSubscription([FromBody] SubscriptionCreateOrUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                // Tự động trả về lỗi 400 với chi tiết validation
                return BadRequest(ModelState);
            }

            var response = await _service.CreateAsync(dto);

            if (!response.Succeeded)
            {
                // Phân loại lỗi trả về từ Service
                if (response.Message == "SM16" || response.Message.Contains("not found"))
                    return BadRequest(response); // 400 Bad Request

                return StatusCode(500, response); // 500 Internal Server Error (SM15 hoặc lỗi khác)
            }

            // Trả về 201 Created với response thành công (SM04 hoặc SM10)
            return CreatedAtAction(nameof(GetSubscription), new { id = response.Data!.SubscriptionId }, response);
        }


        /// <summary>
        /// (UC 2.1.3) Cập nhật một bản nháp gia hạn (Lưu Nháp hoặc Duyệt)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDto>>> PutSubscription(string id, [FromBody] SubscriptionCreateOrUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _service.UpdateAsync(id, dto);

            if (!response.Succeeded)
            {
                if (response.Message == "SM01" || response.Message.Contains("not found"))
                    return NotFound(response); // 404 Not Found

                if (response.Message == "SM16" || response.Message.Contains("Only draft"))
                    return BadRequest(response); // 400 Bad Request

                return StatusCode(500, response); // 500 Internal Server Error (SM15 hoặc lỗi khác)
            }

            // Trả về 200 OK với response thành công (SM03 hoặc SM10)
            return Ok(response);
        }

        /// <summary>
        /// (UC 2.1.4) Xóa một bản nháp gia hạn
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteSubscriptionDraft(string id)
        {
            var response = await _service.DeleteDraftAsync(id);

            if (!response.Succeeded)
            {
                if (response.Message == "SM01")
                    return NotFound(response); // 404 Not Found

                if (response.Message.Contains("Only draft"))
                    return BadRequest(response); // 400 Bad Request

                return StatusCode(500, response); // Lỗi khác
            }

            // Trả về 200 OK với response thành công (SM05)
            return Ok(response);
        }
    }
}