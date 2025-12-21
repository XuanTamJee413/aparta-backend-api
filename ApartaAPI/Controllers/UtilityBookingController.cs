using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static ApartaAPI.DTOs.UtilityBooking.UtilityBookingDtos;

namespace ApartaAPI.Controllers
{
	[Route("api/utilitybookings")]
	[ApiController]
	[Authorize]
	public class UtilityBookingController : ControllerBase
	{
		private readonly IUtilityBookingService _bookingService;

		public UtilityBookingController(IUtilityBookingService bookingService)
		{
			_bookingService = bookingService;
		}

		// Helper lấy ID chuẩn xác
		private string GetCurrentUserId()
		{
			return User.FindFirstValue("id")
				?? User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? User.FindFirstValue("sub")
				?? throw new UnauthorizedAccessException("User ID not found.");
		}

		// --- DÀNH CHO NHÂN VIÊN / ADMIN ---

		// GET: api/utilitybookings (Staff xem list)
		[HttpGet]
		public async Task<ActionResult<PagedList<UtilityBookingDto>>> GetAllBookings([FromQuery] ServiceQueryParameters parameters)
		{
			var staffId = GetCurrentUserId(); // Lấy ID Staff
			var result = await _bookingService.GetAllBookingsAsync(parameters, staffId); // Truyền xuống Service
			return Ok(result);
		}

		// PUT: api/utilitybookings/{id} (Staff duyệt/từ chối)
		[HttpPut("{id}")]
		public async Task<ActionResult<UtilityBookingDto>> UpdateStatus(string id, [FromBody] UtilityBookingUpdateDto updateDto)
		{
			var staffId = GetCurrentUserId(); // Lấy ID Staff
			var result = await _bookingService.UpdateBookingStatusAsync(id, updateDto, staffId); // Truyền xuống Service

			if (!result.Succeeded)
			{
				// Nếu lỗi do không có quyền -> 403 Forbidden
				if (result.Message.Contains("không có quyền")) return StatusCode(403, result);
				if (result.Message == ApiResponse.SM01_NO_RESULTS) return NotFound(result);
				return BadRequest(result);
			}
			return Ok(result);
		}

		// --- CÁC API KHÁC GIỮ NGUYÊN ---
		[HttpPost]
		public async Task<ActionResult<UtilityBookingDto>> CreateBooking([FromBody] UtilityBookingCreateDto createDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);
			var userId = GetCurrentUserId();
			var response = await _bookingService.CreateBookingAsync(createDto, userId);
			if (!response.Succeeded) return BadRequest(response);
			return CreatedAtAction(nameof(GetBookingById), new { id = response.Data!.UtilityBookingId }, response);
		}

		[HttpGet("my")]
		public async Task<ActionResult<IEnumerable<UtilityBookingDto>>> GetMyBookings()
		{
			var userId = GetCurrentUserId();
			var result = await _bookingService.GetBookingsByResidentAsync(userId);
			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<UtilityBookingDto>> GetBookingById(string id)
		{
			var result = await _bookingService.GetBookingByIdAsync(id);
			if (result == null) return NotFound(); // Cẩn thận: result là ApiResponse hay Dto? Service code bạn gửi trả về ApiResponse ở GetBookingByIdAsync, hãy check lại kiểu trả về. Ở code Service trên tôi để ApiResponse.

			// Nếu Service trả ApiResponse:
			if (!result.Succeeded) return NotFound(result);
			return Ok(result.Data);
		}

		// Endpoint dành cho Cư dân tự hủy
		[HttpPut("{id}/cancel")]
		public async Task<IActionResult> CancelBooking(string id)
		{
			var userId = GetCurrentUserId();
			try
			{
				await _bookingService.CancelBookingByResidentAsync(id, userId);
				return Ok(new { message = "Hủy đặt chỗ thành công." });
			}
			catch (KeyNotFoundException) { return NotFound(); }
			catch (UnauthorizedAccessException) { return Forbid(); }
			catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
		}

		// GET: api/utilitybookings/slots?utilityId=...&date=...
		[HttpGet("slots")]
		public async Task<ActionResult<IEnumerable<BookedSlotDto>>> GetBookedSlots(
			[FromQuery] string utilityId,
			[FromQuery] DateTime date)
		{
			var slots = await _bookingService.GetBookedSlotsAsync(utilityId, date);
			return Ok(slots);
		}
	}
}