using ApartaAPI.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.UtilityBooking
{
	public class UtilityBookingDtos
	{
		// DTO tạo mới (Resident)
		public sealed record UtilityBookingCreateDto(
			[Required] string UtilityId,
			[Required] DateTime BookingDate, // Thời gian bắt đầu
			[Required] DateTime BookedAt,    // Thời gian kết thúc
			string? ResidentNote
		);

		// DTO hiển thị (Output)
		public sealed record UtilityBookingDto(
			string UtilityBookingId,
			string UtilityId,
			string UtilityName,
			string ResidentId,
			string ResidentName,
			DateTime BookingDate, // Bắt đầu
			DateTime? BookedAt,   // Kết thúc
			string Status,
			string? ResidentNote,
			string? StaffNote,
			DateTime? CreatedAt
		);

		// DTO cập nhật (Staff)
		public sealed record UtilityBookingUpdateDto(
			[Required] string Status,
			string? StaffNote
		);

		//// Param lọc (Tái sử dụng ServiceQueryParameters hoặc tạo mới nếu muốn)
		//// Ở đây tôi dùng lại cấu trúc giống ServiceQueryParameters
		//public sealed record UtilityBookingQueryParameters : QueryParameters
		//{
		//	public string? Status { get; set; }
		//	public string? SearchTerm { get; set; } // Tìm theo tên Utility
		//}
	}
}