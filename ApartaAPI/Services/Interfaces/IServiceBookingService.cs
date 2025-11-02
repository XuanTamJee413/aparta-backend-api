using ApartaAPI.DTOs.ServiceBooking;

namespace ApartaAPI.Services.Interfaces
{
	public interface IServiceBookingService
	{
		Task<ServiceBookingDtos.ServiceBookingDto> CreateBookingAsync(ServiceBookingDtos.ServiceBookingCreateDto createDto, string residentId);
		Task<IEnumerable<ServiceBookingDtos.ServiceBookingDto>> GetBookingsByResidentAsync(string residentId);
		Task<ServiceBookingDtos.ServiceBookingDto?> GetBookingByIdAsync(string bookingId);
	}
}