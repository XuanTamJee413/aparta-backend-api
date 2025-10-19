namespace ApartaAPI.DTOs.Services
{
	public sealed record ServiceDto(
		string ServiceId,
		string? Name,
		double? Price,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record ServiceCreateDto(
		string? ServiceId,
		string? Name,
		double? Price
	);

	public sealed record ServiceUpdateDto(
		string? Name,
		double? Price
	);
}