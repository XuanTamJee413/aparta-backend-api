namespace ApartaAPI.DTOs.Profile
{
    public class UserProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        
        // Thông tin thêm cho Resident
        public string? ApartmentInfo { get; set; }
        
        // Thông tin thêm cho Manager
        public List<string> ManagedBuildingNames { get; set; } = new List<string>();
    }
}

