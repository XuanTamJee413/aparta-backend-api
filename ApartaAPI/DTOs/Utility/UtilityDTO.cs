namespace ApartaAPI.DTOs.Utilities
{
	public sealed record UtilityDto(
		string UtilityId,
		string? Name,
		string? Status,
		string? Location,
		double? PeriodTime,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record UtilityCreateDto(
		string? Name,
		string? Status,
		string? Location,
		double? PeriodTime
	);

	public sealed record UtilityUpdateDto(
		string? Name,
		string? Status,
		string? Location,
		double? PeriodTime
	);
}