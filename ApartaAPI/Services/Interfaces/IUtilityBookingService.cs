using ApartaAPI.DTOs.Common;
using static ApartaAPI.DTOs.UtilityBooking.UtilityBookingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityBookingService
	{
		Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters, string staffId);

		Task<ApiResponse<UtilityBookingDto>> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto, string staffId);

		Task<ApiResponse<UtilityBookingDto>> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId);
		Task<ApiResponse<UtilityBookingDto>> GetBookingByIdAsync(string bookingId);
		Task<IEnumerable<UtilityBookingDto>> GetBookingsByResidentAsync(string residentId);
		Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(string utilityId, DateTime date);
		Task<ApiResponse> CancelBookingByResidentAsync(string bookingId, string residentId);
	}
}