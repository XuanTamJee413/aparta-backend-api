using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ApartaAPI.DTOs.UtilityBooking.UtilityBookingDtos;

namespace ApartaAPI.Services
{
	public class UtilityBookingService : IUtilityBookingService
	{
		private readonly IRepository<UtilityBooking> _bookingRepository;
		private readonly IRepository<Utility> _utilityRepository;
		private readonly IRepository<User> _userRepository;

		public UtilityBookingService(
			IRepository<UtilityBooking> bookingRepository,
			IRepository<Utility> utilityRepository,
			IRepository<User> userRepository)
		{
			_bookingRepository = bookingRepository;
			_utilityRepository = utilityRepository;
			_userRepository = userRepository;
		}

		public async Task<ApiResponse<UtilityBookingDto>> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId)
		{
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == createDto.UtilityId);
			if (utility == null || utility.Status != "Available")
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM44_SERVICE_NOT_FOUND);
			}

			//if (createDto.BookingDate < DateTime.UtcNow.AddMinutes(30))
			//{
			//	return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM56_BOOKING_MIN_TIME);
			//}

			if (createDto.BookingDate >= createDto.BookedAt)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM45_END_TIME_INVALID);
			}

			if (createDto.BookingDate < DateTime.UtcNow)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM46_PAST_TIME_INVALID);
			}

			// Validate PeriodTime
			if (utility.PeriodTime.HasValue)
			{
				var duration = (createDto.BookedAt - createDto.BookingDate).TotalHours;
				if (duration > utility.PeriodTime.Value)
				{
					return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM47_MAX_DURATION_EXCEEDED.Replace("{hours}", utility.PeriodTime.ToString()));
				}
			}

			var openTime = new TimeSpan(6, 0, 0);
			var closeTime = new TimeSpan(22, 0, 0);

			if (createDto.BookingDate.TimeOfDay < openTime)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM49_OPENING_HOURS_INVALID);
			}

			if (createDto.BookedAt.TimeOfDay > closeTime)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM50_CLOSING_HOURS_INVALID);
			}

			var allBookings = await _bookingRepository.GetAllAsync();

			int pendingOnTargetDate = allBookings.Count(b =>
				b.ResidentId == residentId &&
				b.Status == "Pending" &&
				b.BookingDate.Date == createDto.BookingDate.Date
			);

			if (pendingOnTargetDate >= 1)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM51_PENDING_LIMIT_EXCEEDED);
			}

			var userBookings = allBookings.Where(b => b.ResidentId == residentId).ToList();
			int bookingsOnDay = userBookings.Count(b =>
				b.BookingDate.Date == createDto.BookingDate.Date &&
				(b.Status == "Pending" || b.Status == "Approved")
			);

			if (bookingsOnDay >= 3)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM52_DAILY_BOOKING_LIMIT);
			}

			// Validate Overlap 
			var isOverlapping = allBookings.Any(b =>
				b.UtilityId == createDto.UtilityId &&
				b.Status != "Cancelled" && b.Status != "Rejected" &&
				createDto.BookingDate < b.BookedAt &&
				createDto.BookedAt > b.BookingDate
			);

			if (isOverlapping)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM48_SLOT_OVERLAP);
			}

			var resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == residentId);
			if (resident == null)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND);
			}

			var newBooking = new UtilityBooking
			{
				UtilityBookingId = Guid.NewGuid().ToString(),
				UtilityId = createDto.UtilityId,
				ResidentId = residentId,
				BookingDate = createDto.BookingDate,
				BookedAt = createDto.BookedAt,
				Status = "Pending",
				ResidentNote = createDto.ResidentNote,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _bookingRepository.AddAsync(newBooking);
			await _bookingRepository.SaveChangesAsync();

			newBooking.Utility = utility;
			newBooking.Resident = resident;

			return ApiResponse<UtilityBookingDto>.Success(MapToDto(newBooking));
		}

		public async Task<ApiResponse<UtilityBookingDto>> GetBookingByIdAsync(string bookingId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM01_NO_RESULTS);
			}

			booking.Utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return ApiResponse<UtilityBookingDto>.Success(MapToDto(booking));
		}

		public async Task<ApiResponse<UtilityBookingDto>> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null)
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM01_NO_RESULTS);
			}

			booking.Status = updateDto.Status;
			booking.StaffNote = updateDto.StaffNote;
			booking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(booking);
			await _bookingRepository.SaveChangesAsync();

			booking.Utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return ApiResponse<UtilityBookingDto>.Success(MapToDto(booking), ApiResponse.SM03_UPDATE_SUCCESS);
		}

		public async Task<ApiResponse> CancelBookingByResidentAsync(string bookingId, string residentId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);

			if (booking == null)
			{
				return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
			}

			if (booking.ResidentId != residentId)
			{
				return ApiResponse.Fail(ApiResponse.SM53_CANCEL_DENIED);
			}

			if (booking.BookingDate < DateTime.UtcNow)
			{
				return ApiResponse.Fail(ApiResponse.SM54_CANCEL_EXPIRED);
			}

			if (booking.Status == "Rejected" || booking.Status == "Cancelled")
			{
				return ApiResponse.Fail(ApiResponse.SM55_ALREADY_CANCELLED);
			}

			booking.Status = "Cancelled";
			booking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(booking);
			await _bookingRepository.SaveChangesAsync();

			return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
		}


		public async Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters)
		{
			var allBookings = await _bookingRepository.GetAllAsync();
			var allUtilities = await _utilityRepository.GetAllAsync();
			var allUsers = await _userRepository.GetAllAsync();

			var query = allBookings.Select(b =>
			{
				b.Utility = allUtilities.FirstOrDefault(u => u.UtilityId == b.UtilityId);
				b.Resident = allUsers.FirstOrDefault(u => u.UserId == b.ResidentId);
				return MapToDto(b);
			}).AsQueryable();

			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				query = query.Where(b => b.Status.Equals(parameters.Status.Trim(), StringComparison.OrdinalIgnoreCase));
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				string term = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(b => b.UtilityName.ToLower().Contains(term));
			}

			query = query.OrderByDescending(b => b.CreatedAt);

			var totalCount = query.Count();
			var items = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.ToList();

			return new PagedList<UtilityBookingDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		public async Task<IEnumerable<UtilityBookingDto>> GetBookingsByResidentAsync(string residentId)
		{
			var allBookings = await _bookingRepository.GetAllAsync();
			var allUtilities = await _utilityRepository.GetAllAsync();

			return allBookings
				.Where(b => b.ResidentId == residentId)
				.OrderByDescending(b => b.BookingDate)
				.Select(b => {
					b.Utility = allUtilities.FirstOrDefault(u => u.UtilityId == b.UtilityId);
					return MapToDto(b);
				})
				.ToList();
		}

		public async Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(string utilityId, DateTime date)
		{
			var bookings = await _bookingRepository.GetAllAsync();

			return bookings
				.Where(b =>
					b.UtilityId == utilityId &&
					(b.Status == "Pending" || b.Status == "Approved" || b.Status == "Completed") &&
					b.BookingDate.Date == date.Date
				)
				.Select(b => new BookedSlotDto(b.BookingDate, b.BookedAt ?? b.BookingDate.AddHours(1)))
				.OrderBy(b => b.Start)
				.ToList();
		}

		private UtilityBookingDto MapToDto(UtilityBooking b)
		{
			return new UtilityBookingDto(
				b.UtilityBookingId,
				b.UtilityId,
				b.Utility?.Name ?? "Unknown",
				b.ResidentId,
				b.Resident?.Name ?? "Unknown",
				b.BookingDate,
				b.BookedAt,
				b.Status,
				b.ResidentNote,
				b.StaffNote,
				b.CreatedAt
			);
		}
	}
}