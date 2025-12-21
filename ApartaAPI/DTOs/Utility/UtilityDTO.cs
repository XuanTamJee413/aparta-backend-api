namespace ApartaAPI.DTOs.Utilities
{
	public sealed record UtilityDto(
		string UtilityId,
		string? Name,
		string? Status,
		string? Location,
		double? PeriodTime,
		TimeSpan? OpenTime,  
		TimeSpan? CloseTime,
		string? BuildingId,
		DateTime? CreatedAt,
		DateTime? UpdatedAt
	);

	public sealed record UtilityCreateDto(
		string? Name,
		string? Status,
		string? Location,
		TimeSpan? OpenTime,
		TimeSpan? CloseTime,
		double? PeriodTime,
		string? BuildingId
	);

	public sealed record UtilityUpdateDto(
		string? Name,
		string? Status,
		string? Location,
		TimeSpan? OpenTime,
		TimeSpan? CloseTime,
		double? PeriodTime
	);
}