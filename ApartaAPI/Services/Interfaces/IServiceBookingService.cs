using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.ServiceBooking;
using static ApartaAPI.DTOs.ServiceBooking.ServiceBookingDtos;

namespace ApartaAPI.Services.Interfaces
{
	public interface IServiceBookingService
	{
		Task<ServiceBookingDtos.ServiceBookingDto> CreateBookingAsync(ServiceBookingDtos.ServiceBookingCreateDto createDto, string residentId);
		Task<IEnumerable<ServiceBookingDtos.ServiceBookingDto>> GetBookingsByResidentAsync(string residentId);
		Task<ServiceBookingDtos.ServiceBookingDto?> GetBookingByIdAsync(string bookingId);
		// Dùng PagedList và ServiceQueryParameters (giống template)
		Task<PagedList<ServiceBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters);

		// Tương tự UpdateServiceAsync
		Task<ServiceBookingDto?> UpdateBookingStatusAsync(string bookingId, ServiceBookingUpdateDto updateDto, string operationStaffId);

	}
}