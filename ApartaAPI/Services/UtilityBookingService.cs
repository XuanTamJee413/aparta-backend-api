using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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
		private readonly IMailService _emailService;

		// --- INJECT ---
		private readonly IRepository<StaffBuildingAssignment> _staffAssignmentRepo;
		private readonly IRepository<Apartment> _apartmentRepo; // [MỚI] Thay thế ApartmentMember bằng Apartment

		public UtilityBookingService(
			IRepository<UtilityBooking> bookingRepository,
			IRepository<Utility> utilityRepository,
			IRepository<User> userRepository,
			IMailService emailService,
			IRepository<StaffBuildingAssignment> staffAssignmentRepo,
			IRepository<Apartment> apartmentRepo) // [MỚI]
		{
			_bookingRepository = bookingRepository;
			_utilityRepository = utilityRepository;
			_userRepository = userRepository;
			_emailService = emailService;
			_staffAssignmentRepo = staffAssignmentRepo;
			_apartmentRepo = apartmentRepo;
		}

		// ---------------------------------------------------------
		// 1. LOGIC CƯ DÂN ĐẶT CHỖ (Check cùng tòa nhà)
		// ---------------------------------------------------------
		public async Task<ApiResponse<UtilityBookingDto>> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId)
		{
			// 1.1 Lấy thông tin Utility
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == createDto.UtilityId);
			if (utility == null || utility.Status != "Available")
			{
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM44_SERVICE_NOT_FOUND);
			}

			// 1.2 [LOGIC MỚI - KHÔNG DÙNG APARTMENT MEMBER] 
			// Kiểm tra Tòa nhà: Tiện ích thuộc tòa nào, Cư dân phải ở tòa đó
			if (!string.IsNullOrEmpty(utility.BuildingId))
			{
				// Bước A: Lấy thông tin User để biết đang ở ApartmentId nào
				var user = await _userRepository.GetByIdAsync(residentId);

				if (user == null || string.IsNullOrEmpty(user.ApartmentId))
				{
					return ApiResponse<UtilityBookingDto>.Fail("Bạn chưa được gán vào căn hộ nào.");
				}

				// Bước B: Lấy thông tin Apartment để biết BuildingId
				var apartment = await _apartmentRepo.GetByIdAsync(user.ApartmentId);

				if (apartment == null)
				{
					return ApiResponse<UtilityBookingDto>.Fail("Không tìm thấy thông tin căn hộ.");
				}

				// Bước C: So sánh
				if (apartment.BuildingId != utility.BuildingId)
				{
					return ApiResponse<UtilityBookingDto>.Fail("Bạn chỉ có thể đặt tiện ích thuộc tòa nhà của mình.");
				}
			}

			// 1.3 Validate Thời gian
			if (createDto.BookingDate < DateTime.UtcNow.AddMinutes(60))
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM56_BOOKING_MIN_TIME);

			if (createDto.BookingDate >= createDto.BookedAt)
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM45_END_TIME_INVALID);

			if (createDto.BookingDate < DateTime.UtcNow)
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM46_PAST_TIME_INVALID);

			if (utility.PeriodTime.HasValue)
			{
				var duration = (createDto.BookedAt - createDto.BookingDate).TotalHours;
				if (duration > utility.PeriodTime.Value)
					return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM47_MAX_DURATION_EXCEEDED.Replace("{hours}", utility.PeriodTime.ToString()));
			}

			if (createDto.BookingDate.TimeOfDay < utility.OpenTime)
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM49_OPENING_HOURS_INVALID.Replace("{hours}", utility.OpenTime.ToString()));

			if (createDto.BookedAt.TimeOfDay > utility.CloseTime)
				return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM50_CLOSING_HOURS_INVALID.Replace("{hours}", utility.CloseTime.ToString()));

			// 1.4 Validate Logic nghiệp vụ khác
			var allBookings = await _bookingRepository.GetAllAsync();
			var userBookings = allBookings.Where(b => b.ResidentId == residentId).ToList();

			int bookingsOnDay = userBookings.Count(b =>
				b.BookingDate.Date == createDto.BookingDate.Date &&
				(b.Status == "Pending" || b.Status == "Approved")
			);

			if (bookingsOnDay >= 3) return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM52_DAILY_BOOKING_LIMIT);

			var isOverlapping = allBookings.Any(b =>
				b.UtilityId == createDto.UtilityId &&
				b.Status != "Cancelled" && b.Status != "Rejected" &&
				createDto.BookingDate < b.BookedAt &&
				createDto.BookedAt > b.BookingDate
			);

			if (isOverlapping) return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM48_SLOT_OVERLAP);

			// Nếu biến user đã lấy ở trên thì dùng lại, chưa thì lấy
			var resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == residentId);

			// 1.5 Tạo Booking
			var newBooking = new UtilityBooking
			{
				UtilityBookingId = Guid.NewGuid().ToString(),
				UtilityId = createDto.UtilityId,
				ResidentId = residentId,
				BookingDate = createDto.BookingDate,
				BookedAt = createDto.BookedAt,
				Status = "Approved",
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

		// ---------------------------------------------------------
		// 2. LOGIC STAFF XEM DANH SÁCH (Chỉ xem Booking tòa mình quản lý)
		// ---------------------------------------------------------
		public async Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters, string staffId)
		{
			// 2.1 Lấy danh sách BuildingId mà Staff này quản lý
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			// Sử dụng FindAsync để tránh lỗi GetQueryable nếu repo chưa update
			var assignments = await _staffAssignmentRepo.FindAsync(x =>
				x.UserId == staffId &&
				x.IsActive == true &&
				(x.AssignmentEndDate == null || x.AssignmentEndDate >= today)
			);
			var allowedBuildingIds = assignments.Select(x => x.BuildingId).ToList();

			if (!allowedBuildingIds.Any())
			{
				return new PagedList<UtilityBookingDto>(new List<UtilityBookingDto>(), 0, parameters.PageNumber, parameters.PageSize);
			}

			// 2.2 Lấy dữ liệu và Filter
			// Lưu ý: Nếu Repo chưa hỗ trợ GetQueryable(), ta nên lấy hết về rồi lọc (Client-side evaluation)
			// Để an toàn nhất, tôi sẽ dùng cách lấy hết và lọc trên RAM
			var allBookings = await _bookingRepository.GetAllAsync();
			var allUtilities = await _utilityRepository.GetAllAsync();
			var allUsers = await _userRepository.GetAllAsync();

			var query = from b in allBookings
						join u in allUtilities on b.UtilityId equals u.UtilityId
						join r in allUsers on b.ResidentId equals r.UserId
						where u.BuildingId != null && allowedBuildingIds.Contains(u.BuildingId) // <--- CHECK QUYỀN STAFF
						select new { Booking = b, Utility = u, Resident = r };

			// 2.3 Filter Params
			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				var status = parameters.Status.Trim();
				query = query.Where(x => x.Booking.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				string term = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(x => x.Utility.Name.ToLower().Contains(term));
			}

			// 2.4 Sorting & Paging
			var totalCount = query.Count();
			var itemsRaw = query
				.OrderByDescending(x => x.Booking.CreatedAt)
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.ToList();

			// 2.5 Map to DTO
			var items = itemsRaw.Select(x => {
				x.Booking.Utility = x.Utility;
				x.Booking.Resident = x.Resident;
				return MapToDto(x.Booking);
			}).ToList();

			return new PagedList<UtilityBookingDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		// ---------------------------------------------------------
		// 3. LOGIC STAFF UPDATE (Chỉ update Booking tòa mình quản lý)
		// ---------------------------------------------------------
		public async Task<ApiResponse<UtilityBookingDto>> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto, string staffId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null) return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM01_NO_RESULTS);

			// Load Utility để check BuildingId
			var utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			if (utility == null) return ApiResponse<UtilityBookingDto>.Fail("Không tìm thấy tiện ích liên quan.");

			// 3.1 Check quyền: Staff có quản lý tòa nhà của tiện ích này không?
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			// Dùng FindAsync để an toàn
			var assignments = await _staffAssignmentRepo.FindAsync(x =>
				x.UserId == staffId &&
				x.IsActive == true &&
				x.BuildingId == utility.BuildingId && // Match Building
				(x.AssignmentEndDate == null || x.AssignmentEndDate >= today)
			);

			if (!assignments.Any())
			{
				return ApiResponse<UtilityBookingDto>.Fail("Bạn không có quyền duyệt đơn đặt của tòa nhà này.");
			}

			// 3.2 Update Logic
			booking.Status = updateDto.Status;
			booking.StaffNote = updateDto.StaffNote;
			booking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(booking);
			await _bookingRepository.SaveChangesAsync();

			// Load Resident info for email
			booking.Utility = utility;
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			// 3.3 Gửi Email
			if (booking.Resident != null && !string.IsNullOrEmpty(booking.Resident.Email))
			{
				try
				{
					string emailSubject = $"[Aparta] Cập nhật trạng thái đặt tiện ích: {booking.Utility?.Name}";
					string statusVn = TranslateStatus(booking.Status);
					string emailBody = $@"
						<h3>Xin chào {booking.Resident.Name},</h3>
						<p>Yêu cầu đặt tiện ích <strong>{booking.Utility?.Name}</strong> của bạn...
						<p><strong>Trạng thái mới:</strong> <span style='color:blue; font-weight:bold'>{statusVn}</span></p>";
					await _emailService.SendEmailAsync(booking.Resident.Email, emailSubject, emailBody);
				}
				catch { }
			}

			return ApiResponse<UtilityBookingDto>.Success(MapToDto(booking), ApiResponse.SM03_UPDATE_SUCCESS);
		}

		// --- CÁC HÀM KHÁC GIỮ NGUYÊN ---
		public async Task<ApiResponse<UtilityBookingDto>> GetBookingByIdAsync(string bookingId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null) return ApiResponse<UtilityBookingDto>.Fail(ApiResponse.SM01_NO_RESULTS);

			booking.Utility = await _utilityRepository.FirstOrDefaultAsync(u => u.UtilityId == booking.UtilityId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return ApiResponse<UtilityBookingDto>.Success(MapToDto(booking));
		}

		public async Task<ApiResponse> CancelBookingByResidentAsync(string bookingId, string residentId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.UtilityBookingId == bookingId);
			if (booking == null) return ApiResponse.Fail(ApiResponse.SM01_NO_RESULTS);
			if (booking.ResidentId != residentId) return ApiResponse.Fail(ApiResponse.SM53_CANCEL_DENIED);

			if (booking.BookingDate < DateTime.UtcNow) return ApiResponse.Fail(ApiResponse.SM54_CANCEL_EXPIRED);
			if (booking.Status == "Rejected" || booking.Status == "Cancelled") return ApiResponse.Fail(ApiResponse.SM55_ALREADY_CANCELLED);

			booking.Status = "Cancelled";
			booking.UpdatedAt = DateTime.UtcNow;
			await _bookingRepository.UpdateAsync(booking);
			await _bookingRepository.SaveChangesAsync();
			return ApiResponse.Success(ApiResponse.SM03_UPDATE_SUCCESS);
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
				}).ToList();
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

		private string TranslateStatus(string status) => status switch
		{
			"Pending" => "Đang chờ duyệt",
			"Approved" => "Đã duyệt",
			"Rejected" => "Đã từ chối",
			"Cancelled" => "Đã hủy",
			"Completed" => "Đã hoàn thành",
			_ => status
		};

		private UtilityBookingDto MapToDto(UtilityBooking b) => new UtilityBookingDto(
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