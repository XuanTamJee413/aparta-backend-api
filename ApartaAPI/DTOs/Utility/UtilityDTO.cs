namespace ApartaAPI.DTOs.Utilities
{
	public sealed record UtilityDto(
		string UtilityId,
		string? Name,
		string? Status,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record UtilityCreateDto(
		string? UtilityId,
		string? Name,
		string? Status
	);

	public sealed record UtilityUpdateDto(
		string? Name,
		string? Status
	);
}