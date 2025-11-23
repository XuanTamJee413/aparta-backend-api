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

		private string GetCurrentUserId()
		{
			return User.FindFirstValue("id")
				?? User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? User.FindFirstValue("sub")
				?? throw new UnauthorizedAccessException("User ID not found.");
		}

		// --- DÀNH CHO CƯ DÂN ---

		// POST: api/utilitybookings
		[HttpPost]
		[ProducesResponseType(typeof(UtilityBookingDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<UtilityBookingDto>> CreateBooking([FromBody] UtilityBookingCreateDto createDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetCurrentUserId();
			try
			{
				var result = await _bookingService.CreateBookingAsync(createDto, userId);
				return CreatedAtAction(nameof(GetBookingById), new { id = result.UtilityBookingId }, result);
			}
			catch (ArgumentException ex) // Lỗi logic thời gian
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (InvalidOperationException ex) // Lỗi trùng lặp hoặc không tồn tại
			{
				return Conflict(new { message = ex.Message }); // 409 Conflict
			}
		}

		// GET: api/utilitybookings/my
		[HttpGet("my")]
		public async Task<ActionResult<IEnumerable<UtilityBookingDto>>> GetMyBookings()
		{
			var userId = GetCurrentUserId();
			var result = await _bookingService.GetBookingsByResidentAsync(userId);
			return Ok(result);
		}

		// --- DÀNH CHO NHÂN VIÊN / ADMIN ---

		// GET: api/utilitybookings
		[HttpGet]
		public async Task<ActionResult<PagedList<UtilityBookingDto>>> GetAllBookings([FromQuery] ServiceQueryParameters parameters)
		{
			var result = await _bookingService.GetAllBookingsAsync(parameters);
			return Ok(result);
		}

		// PUT: api/utilitybookings/{id}
		[HttpPut("{id}")]
		public async Task<ActionResult<UtilityBookingDto>> UpdateStatus(string id, [FromBody] UtilityBookingUpdateDto updateDto)
		{
			var result = await _bookingService.UpdateBookingStatusAsync(id, updateDto);
			if (result == null) return NotFound();
			return Ok(result);
		}

		// --- DÙNG CHUNG ---

		// GET: api/utilitybookings/{id}
		[HttpGet("{id}")]
		public async Task<ActionResult<UtilityBookingDto>> GetBookingById(string id)
		{
			var result = await _bookingService.GetBookingByIdAsync(id);
			if (result == null) return NotFound();
			return Ok(result);
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