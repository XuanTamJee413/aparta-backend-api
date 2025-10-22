namespace ApartaAPI.DTOs.Services
{
	public sealed record ServiceDto(
		string ServiceId,
		string? Name,
		decimal? Price,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record ServiceCreateDto(
		string? ServiceId,
		string? Name,
		decimal? Price
	);

	public sealed record ServiceUpdateDto(
		string? Name,
		decimal? Price
	);
}