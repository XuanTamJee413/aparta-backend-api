using ApartaAPI.DTOs.Common;
using static ApartaAPI.DTOs.UtilityBooking.UtilityBookingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityBookingService
	{
		// Cư dân đặt chỗ
		Task<UtilityBookingDto> CreateBookingAsync(UtilityBookingCreateDto createDto, string residentId);

		// Cư dân xem lịch sử
		Task<IEnumerable<UtilityBookingDto>> GetBookingsByResidentAsync(string residentId);

		// Staff xem tất cả (Phân trang)
		Task<PagedList<UtilityBookingDto>> GetAllBookingsAsync(ServiceQueryParameters parameters);

		// Xem chi tiết
		Task<UtilityBookingDto?> GetBookingByIdAsync(string bookingId);

		// Staff duyệt/hủy
		Task<UtilityBookingDto?> UpdateBookingStatusAsync(string bookingId, UtilityBookingUpdateDto updateDto);

		Task<bool> CancelBookingByResidentAsync(string bookingId, string residentId);
	}
}