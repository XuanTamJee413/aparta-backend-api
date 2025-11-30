using ApartaAPI.DTOs.Common;
using static ApartaAPI.DTOs.UtilityBooking.UtilityBookingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityBookingService
	{
		Task<ApiResponse<UtilityBookingDto>> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId);
        Task<ApiResponse<UtilityBookingDto>> GetBookingByIdAsync(string bookingId);
        Task<ApiResponse<UtilityBookingDto>> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto);
        Task<ApiResponse> CancelBookingByResidentAsync(string bookingId, string residentId);

        // Các hàm danh sách giữ nguyên (hoặc có thể bọc nếu muốn, nhưng PagedList thường dùng độc lập)
        Task<IEnumerable<UtilityBookingDto>> GetBookingsByResidentAsync(string residentId);
        Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters);
        Task<IEnumerable<BookedSlotDto>> GetBookedSlotsAsync(string utilityId, DateTime date);
	}
}