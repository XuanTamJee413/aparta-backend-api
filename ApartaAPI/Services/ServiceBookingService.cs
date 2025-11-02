using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ApartaAPI.DTOs.ServiceBooking.ServiceBookingDtos;

namespace ApartaAPI.Services
{
	public class ServiceBookingService : IServiceBookingService
	{
		private readonly IRepository<ServiceBooking> _bookingRepository;
		private readonly IRepository<Service> _serviceRepository;
		private readonly IRepository<User> _userRepository;

		public ServiceBookingService(
			IRepository<ServiceBooking> bookingRepository,
			IRepository<Service> serviceRepository,
			IRepository<User> userRepository)
		{
			_bookingRepository = bookingRepository;
			_serviceRepository = serviceRepository;
			_userRepository = userRepository;
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