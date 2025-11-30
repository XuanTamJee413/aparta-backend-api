using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.User
{
	public sealed record StaffDto(
        string UserId,
        string Name,
        string Role
    );
    public class StaffCreateDto
    {
        [Required] public string Name { get; set; } = null!;
        [Required] public string Email { get; set; } = null!;
        [Required] public string Phone { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
        [Required] public string RoleId { get; set; } = null!;
        public string? StaffCode { get; set; }
        // Thêm các trường cần thiết khác
    }
    public class AssignmentUpdateDto
    {
        [Required] public List<string> BuildingIds { get; set; } = new List<string>();
        public string? ScopeOfWork { get; set; }
    }
    public class StatusUpdateDto
    {
        [Required] public string Status { get; set; } = null!; // Ví dụ: "Active", "Inactive"
    }
}
