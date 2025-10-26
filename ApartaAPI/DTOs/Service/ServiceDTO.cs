namespace ApartaAPI.DTOs.Services
{
	public sealed record ServiceDto(
		string ServiceId,
		string? Name,
		decimal? Price,
		string? Status,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record ServiceCreateDto(
		string? Name,
		decimal? Price,
		string? Status
	);

	public sealed record ServiceUpdateDto(
		string? Name,
		decimal? Price,
		string? Status
	);
}