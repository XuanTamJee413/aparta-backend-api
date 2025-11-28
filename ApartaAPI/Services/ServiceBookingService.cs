using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using static ApartaAPI.DTOs.ServiceBooking.ServiceBookingDtos;
using Task = ApartaAPI.Models.Task;

namespace ApartaAPI.Services
{
	public class ServiceBookingService : IServiceBookingService
	{
		private readonly IRepository<ServiceBooking> _bookingRepository;
		private readonly IRepository<Service> _serviceRepository;
		private readonly IRepository<User> _userRepository;
		private readonly ITaskService _taskService;
		public ServiceBookingService(
			IRepository<ServiceBooking> bookingRepository,
			IRepository<Service> serviceRepository,
			IRepository<User> userRepository,
			ITaskService taskService)

		{
			_bookingRepository = bookingRepository;
			_serviceRepository = serviceRepository;
			_userRepository = userRepository;
			_taskService = taskService;
		}

		public async Task<ServiceBookingDto> CreateBookingAsync(ServiceBookingCreateDto createDto, string residentId)
		{
			var service = await _serviceRepository.FirstOrDefaultAsync(
				s => s.ServiceId == createDto.ServiceId && s.Status == "Available");

			if (service == null)
			{
				throw new InvalidOperationException("Dịch vụ không tồn tại hoặc không khả dụng.");
			}

			var resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == residentId); 
			if (resident == null)
			{
				throw new InvalidOperationException("Không tìm thấy cư dân.");
			}

			if (createDto.BookingDate < DateTime.UtcNow.Date)
			{
				throw new InvalidOperationException("Không thể đặt dịch vụ cho ngày trong quá khứ.");
			}

			var newBooking = new ServiceBooking
			{
				ServiceBookingId = Guid.NewGuid().ToString(),
				ServiceId = createDto.ServiceId,
				ResidentId = residentId,
				BookingDate = createDto.BookingDate,
				Status = "Pending",
				PaymentAmount = service.Price,
				ResidentNote = createDto.ResidentNote, 
				StaffNote = null, 
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			var addedBooking = await _bookingRepository.AddAsync(newBooking);
			await _bookingRepository.SaveChangesAsync();

			addedBooking.Service = service;
			addedBooking.Resident = resident;

			return MapToDto(addedBooking);
		}

		public async Task<IEnumerable<ServiceBookingDto>> GetBookingsByResidentAsync(string residentId)
		{
			var allBookings = await _bookingRepository.GetAllAsync();
			var allServices = await _serviceRepository.GetAllAsync();
			var allUsers = await _userRepository.GetAllAsync();

			var residentBookings = allBookings
				.Where(b => b.ResidentId == residentId)
				.OrderByDescending(b => b.BookingDate);

			var dtos = residentBookings.Select(b =>
			{
				b.Service = allServices.FirstOrDefault(s => s.ServiceId == b.ServiceId);
				b.Resident = allUsers.FirstOrDefault(u => u.UserId == b.ResidentId); 
				return MapToDto(b); 
			});

			return dtos;
		}

		public async Task<ServiceBookingDto?> GetBookingByIdAsync(string bookingId)
		{
			var booking = await _bookingRepository.FirstOrDefaultAsync(b => b.ServiceBookingId == bookingId);
			if (booking == null) return null;

			booking.Service = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == booking.ServiceId);
			booking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == booking.ResidentId);

			return MapToDto(booking);
		}

		public async Task<PagedList<ServiceBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters)
		{
			// Tải tất cả dữ liệu (theo pattern của bạn)
			var allBookings = await _bookingRepository.GetAllAsync();
			var allServices = await _serviceRepository.GetAllAsync();
			var allUsers = await _userRepository.GetAllAsync();

			// Join thủ công vào bộ nhớ TRƯỚC KHI LỌC
			var query = allBookings.Select(b =>
			{
				b.Service = allServices.FirstOrDefault(s => s.ServiceId == b.ServiceId);
				b.Resident = allUsers.FirstOrDefault(u => u.UserId == b.ResidentId);
				return MapToDto(b); // Map sang DTO để lọc
			}).AsQueryable();

			// Lọc theo Trạng thái (Status)
			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				var status = parameters.Status.Trim();
				query = query.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
			}

			// Lọc theo Tên dịch vụ (SearchTerm)
			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(b => b.ServiceName != null && b.ServiceName.ToLower().Contains(searchTerm));
			}

			// Sắp xếp
			query = query.OrderByDescending(b => b.CreatedAt);

			// Phân trang
			var totalCount = query.Count();
			var pagedItems = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.ToList();

			return new PagedList<ServiceBookingDto>(pagedItems, totalCount, parameters.PageNumber, parameters.PageSize);
		}

		// 2. Cập nhật Booking (giống template)
		public async Task<ServiceBookingDto?> UpdateBookingStatusAsync(string bookingId, ServiceBookingUpdateDto updateDto, string operationStaffId)
		{
			var existingBooking = await _bookingRepository.FirstOrDefaultAsync(b => b.ServiceBookingId == bookingId);

			if (existingBooking == null)
			{
				return null;
			}

			// Lưu trạng thái cũ để so sánh
			string oldStatus = existingBooking.Status;

			existingBooking.Status = updateDto.Status ?? existingBooking.Status;
			existingBooking.PaymentAmount = updateDto.PaymentAmount ?? existingBooking.PaymentAmount;
			existingBooking.StaffNote = updateDto.StaffNote ?? existingBooking.StaffNote;
			existingBooking.UpdatedAt = DateTime.UtcNow;

			await _bookingRepository.UpdateAsync(existingBooking);
			await _bookingRepository.SaveChangesAsync();

			// --- LOGIC MỚI: TỰ ĐỘNG TẠO TASK ---
			// Nếu trạng thái chuyển sang Approved/Confirmed VÀ trước đó chưa phải trạng thái này
			if ((existingBooking.Status == "Approved" || existingBooking.Status == "Confirmed") && oldStatus != existingBooking.Status)
			{
				// Load thêm thông tin Service để làm Description cho rõ
				var serviceInfo = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == existingBooking.ServiceId);
				string serviceName = serviceInfo?.Name ?? "Dịch vụ";

				string description = $"Thực hiện: {serviceName}. Ghi chú KH: {existingBooking.ResidentNote}";

				// Gọi TaskService để tạo Task (Status = New)
				// Hàm này bạn đã thêm ở bước trước trong ITaskService
				await _taskService.CreateTaskFromBookingAsync(existingBooking.ServiceBookingId, description, operationStaffId);
			}
			// ------------------------------------

			existingBooking.Service = await _serviceRepository.FirstOrDefaultAsync(s => s.ServiceId == existingBooking.ServiceId);
			existingBooking.Resident = await _userRepository.FirstOrDefaultAsync(u => u.UserId == existingBooking.ResidentId);

			return MapToDto(existingBooking);
		}


		private ServiceBookingDto MapToDto(ServiceBooking booking)
		{
			string residentName = booking.Resident?.Name ?? "N/A";

			return new ServiceBookingDto(
				booking.ServiceBookingId,
				booking.ServiceId,
				booking.Service?.Name ?? "N/A",
				booking.ResidentId,
				residentName,
				booking.BookingDate,
				booking.Status,
				booking.PaymentAmount,
				booking.ResidentNote, 
				booking.StaffNote,    
				booking.CreatedAt
			);
		}
	}
}