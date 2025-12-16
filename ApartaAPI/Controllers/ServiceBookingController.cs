using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static ApartaAPI.DTOs.ServiceBooking.ServiceBookingDtos;

namespace ApartaAPI.Controllers
{
	[Route("api/servicebookings")]
	public class ServiceBookingController : ControllerBase
	{
		private readonly IServiceBookingService _bookingService;

		public ServiceBookingController(IServiceBookingService bookingService)
		{
			_bookingService = bookingService;
		}

		private string GetCurrentUserId()
		{
			return User.FindFirstValue("id")
				?? User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? User.FindFirstValue("sub")
				?? throw new UnauthorizedAccessException("User ID not found in token.");
		}

		// POST: api/servicebookings
		[HttpPost]
		[ProducesResponseType(typeof(ServiceBookingDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ServiceBookingDto>> CreateBooking([FromBody] ServiceBookingCreateDto createDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var residentId = GetCurrentUserId();

			try
			{
				var createdBooking = await _bookingService.CreateBookingAsync(createDto, residentId);

				return CreatedAtAction(
					nameof(GetBookingById),
					new { id = createdBooking.ServiceBookingId },
					createdBooking
				);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// GET: api/servicebookings/my
		[HttpGet("my")]
		[ProducesResponseType(typeof(IEnumerable<ServiceBookingDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<ServiceBookingDto>>> GetMyBookings()
		{
			var residentId = GetCurrentUserId();

			var bookings = await _bookingService.GetBookingsByResidentAsync(residentId);
			return Ok(bookings);
		}

		// GET: api/servicebookings/{id}
		[HttpGet("{id}")]
		[ProducesResponseType(typeof(ServiceBookingDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<ActionResult<ServiceBookingDto>> GetBookingById(string id)
		{
			var booking = await _bookingService.GetBookingByIdAsync(id);
			if (booking == null)
			{
				return NotFound();
			}

			return Ok(booking);
		}


		// OPERATION STAFF
	

		// GET: api/servicebookings
		/// <summary>
		/// (Staff) Lấy tất cả service bookings (có lọc và phân trang).
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(PagedList<ServiceBookingDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<PagedList<ServiceBookingDto>>> GetAllBookings(
			[FromQuery] ServiceQueryParameters parameters)
		{
			var bookings = await _bookingService.GetAllBookingsAsync(parameters);
			return Ok(bookings);
		}


		// PUT: api/servicebookings/{id}
		/// <summary>
		/// (Staff) Cập nhật trạng thái/giá của một booking (Duyệt/Từ chối/Hoàn thành).
		/// </summary>
		[HttpPut("{id}")]
		[ProducesResponseType(typeof(ServiceBookingDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ServiceBookingDto>> UpdateBookingStatus(string id, [FromBody] ServiceBookingUpdateDto updateDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			try
			{
				var staffId = GetCurrentUserId(); // Lấy ID của OS đang đăng nhập

				// Truyền staffId vào
				var updatedBooking = await _bookingService.UpdateBookingStatusAsync(id, updateDto, staffId);

				if (updatedBooking == null) return NotFound();

				return Ok(updatedBooking);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpPut("{id}/cancel")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> CancelBooking(string id)
		{
			try
			{
				var residentId = GetCurrentUserId(); // Lấy ID user đang đăng nhập

				var result = await _bookingService.CancelBookingAsync(id, residentId);

				if (result)
				{
					return NoContent(); // 204: Thành công, không cần trả về nội dung
				}

				return BadRequest(new { message = "Không thể hủy đơn đặt này." });
			}
			catch (KeyNotFoundException)
			{
				return NotFound(new { message = "Không tìm thấy đơn đặt." });
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(403, new { message = ex.Message }); // Trả về 403 Forbidden
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message }); // Trả về lỗi logic (VD: Không phải Pending)
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
			}
		}
	}
}