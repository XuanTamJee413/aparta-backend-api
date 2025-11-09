

namespace ApartaAPI.DTOs.ServiceBooking
{
	public class ServiceBookingDtos
	{
		public sealed record ServiceBookingCreateDto(
		string ServiceId,
		DateTime BookingDate,
		string? ResidentNote 
	);

		public sealed record ServiceBookingDto(
			string ServiceBookingId,
			string ServiceId,
			string ServiceName, 
			string ResidentId,
			string ResidentName, 
			DateTime BookingDate,
			string Status,
			decimal? PaymentAmount,
			string? ResidentNote, 
			string? StaffNote,    
			DateTime? CreatedAt
		);

		public sealed record ServiceBookingUpdateDto(
			string Status,
			decimal? PaymentAmount,
			string? StaffNote
		);

	}
}
