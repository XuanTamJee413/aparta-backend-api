namespace ApartaAPI.DTOs.User
{
	public sealed record StaffDto(
        string UserId,
        string Name,
        string Role
    );
}
