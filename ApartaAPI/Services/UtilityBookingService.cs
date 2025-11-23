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

		public async Task<UtilityBookingDto> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId)
		{
			// 1. Kiểm tra Utility có tồn tại và Active không
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == createDto.UtilityId);
			if (utility == null || utility.Status != "Available")
			{
				throw new InvalidOperationException("Tiện ích không tồn tại hoặc đang bảo trì.");
			}

			// 2. Kiểm tra logic thời gian cơ bản
			if (createDto.BookingDate >= createDto.BookedAt)
			{
				throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.");
			}
			if (createDto.BookingDate < DateTime.UtcNow)
			{
				throw new ArgumentException("Không thể đặt lịch trong quá khứ.");
			}

			// 3. Validate PeriodTime (Tổng thời gian không được quá quy định)
			// Giả sử PeriodTime là số giờ (double)
			if (utility.PeriodTime.HasValue)
			{
				var duration = (createDto.BookedAt - createDto.BookingDate).TotalHours;
				if (duration > utility.PeriodTime.Value)
				{
					throw new ArgumentException($"Thời gian đặt tối đa cho tiện ích này là {utility.PeriodTime} giờ.");
				}
			}

			// 4. Validate Overlap (Kiểm tra trùng lịch)
			// Logic trùng: (StartA < EndB) AND (EndA > StartB)
			// Chúng ta cần kiểm tra DB xem có booking nào đã Confirm/Pending mà trùng giờ không
			var allBookings = await _bookingRepository.GetAllAsync(); // Tối ưu: Nên dùng _repo.Find(condition)

			var isOverlapping = allBookings.Any(b =>
				b.UtilityId == createDto.UtilityId &&
				b.Status != "Cancelled" && b.Status != "Rejected" && // Bỏ qua các đơn đã hủy
				createDto.BookingDate < b.BookedAt &&
				createDto.BookedAt > b.BookingDate
			);

			if (isOverlapping)
			{
				throw new InvalidOperationException("Khung giờ này đã có người đặt. Vui lòng chọn giờ khác.");
			}

			// 5. Tạo Booking
			var resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == residentId);
			if (resident == null) throw new InvalidOperationException("Cư dân không xác định.");

			var newBooking = new UtilityBooking
			{
				UtilityBookingId = Guid.NewGuid().ToString(),
				UtilityId = createDto.UtilityId,
				ResidentId = residentId,
				BookingDate = createDto.BookingDate, // Bắt đầu
				BookedAt = createDto.BookedAt,       // Kết thúc
				Status = "Pending",
				ResidentNote = createDto.ResidentNote,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _bookingRepository.AddAsync(newBooking);
			await _bookingRepository.SaveChangesAsync();

			// Map data để trả về
			newBooking.Utility = utility;
			newBooking.Resident = resident;

			return MapToDto(newBooking);
		}

		public async Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters)
		{
			var allBookings = await _bookingRepository.GetAllAsync();
			var allUtilities = await _utilityRepository.GetAllAsync();
			var allUsers = await _userRepository.GetAllAsync();

			// Join in-memory
			var query = allBookings.Select(b =>
			{
				b.Utility = allUtilities.FirstOrDefault(u => u.UtilityId == b.UtilityId);
				b.Resident = allUsers.FirstOrDefault(u => u.UserId == b.ResidentId);
				return MapToDto(b);
			}).AsQueryable();

			// Filter Status
			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				query = query.Where(b => b.Status.Equals(parameters.Status.Trim(), StringComparison.OrdinalIgnoreCase));
			}

			// Search Term (Theo tên tiện ích)
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
			var allUsers = await _userRepository.GetAllAsync(); // Chỉ để lấy tên nếu cần

			return allBookings
				.Where(b => b.ResidentId == residentId)
				.OrderByDescending(b => b.BookingDate)
				.Select(b => {
					b.Utility = allUtilities.FirstOrDefault(u => u.UtilityId == b.UtilityId);
					return MapToDto(b);
				})
				.ToList();
		}

		public async Task<UtilityBookingDto?> GetBookingByIdAsync(string bookingId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null) return null;

			booking.Utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return MapToDto(booking);
		}

		public async Task<UtilityBookingDto?> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null) return null;

			booking.Status = updateDto.Status;
			booking.StaffNote = updateDto.StaffNote;
			booking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(booking);
			await _bookingRepository.SaveChangesAsync();

			// Re-fetch info for DTO
			booking.Utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return MapToDto(booking);
		}

		public async Task<bool> CancelBookingByResidentAsync(string bookingId, string residentId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);

			if (booking == null) throw new KeyNotFoundException("Đơn đặt không tồn tại.");

			// 1. Kiểm tra chính chủ
			if (booking.ResidentId != residentId)
			{
				throw new UnauthorizedAccessException("Bạn không có quyền hủy đơn này.");
			}

			// 2. Kiểm tra thời gian (Khóa nếu đã quá hạn)
			// Nếu thời gian bắt đầu < thời gian hiện tại => Đã quá hạn
			if (booking.BookingDate < DateTime.UtcNow)
			{
				throw new InvalidOperationException("Đã quá thời gian bắt đầu, không thể hủy.");
			}

			// 3. Chỉ cho phép hủy nếu chưa bị Reject hoặc đã Cancel rồi
			if (booking.Status == "Rejected" || booking.Status == "Cancelled")
			{
				throw new InvalidOperationException("Đơn này đã bị hủy hoặc từ chối trước đó.");
			}

			booking.Status = "Cancelled";
			booking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(booking);
			return await _bookingRepository.SaveChangesAsync();
		}

		public async Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(string utilityId, DateTime date)
		{
			// Lấy tất cả booking của Utility đó
			// Trạng thái: Chỉ lấy Pending, Approved (Confirmed), Completed. Bỏ qua Cancelled/Rejected.
			var bookings = await _bookingRepository.GetAllAsync();

			return bookings
				.Where(b =>
					b.UtilityId == utilityId &&
					(b.Status == "Pending" || b.Status == "Approved" || b.Status == "Completed") &&
					b.BookingDate.Date == date.Date // So sánh cùng ngày
				)
				.Select(b => new BookedSlotDto(b.BookingDate, b.BookedAt ?? b.BookingDate.AddHours(1))) // Giả sử nếu null thì +1h
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