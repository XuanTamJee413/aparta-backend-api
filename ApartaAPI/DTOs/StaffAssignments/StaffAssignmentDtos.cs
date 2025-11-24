using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.StaffAssignments
{
    // 1. DTO dùng để Gán nhân viên (Create)
    public sealed record StaffAssignmentCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
        public string UserId { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn tòa nhà.")]
        public string BuildingId { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập vị trí/chức danh.")]
        public string Position { get; set; } = null!; // VD: Trưởng ca, Nhân viên kỹ thuật

        public string? ScopeOfWork { get; set; } // Phạm vi công việc

        [Required]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }

    // 2. DTO dùng để Cập nhật trạng thái/Kết thúc phân công (Update)
    public sealed record StaffAssignmentUpdateDto
    {
        public string? Position { get; set; }
        public string? ScopeOfWork { get; set; }
        public DateOnly? EndDate { get; set; } // Nếu có giá trị -> Kết thúc công tác
        public bool IsActive { get; set; }
    }

    // 3. DTO hiển thị thông tin (Read) - Đã Join bảng User và Building
    public sealed record StaffAssignmentDto
    {
        public string AssignmentId { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string StaffName { get; set; } = null!; // Tên nhân viên
        public string StaffCode { get; set; } = null!;
        
        public string BuildingId { get; set; } = null!;
        public string BuildingName { get; set; } = null!;
        
        public string Position { get; set; } = null!;
        public string? ScopeOfWork { get; set; }
        
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        
        public bool IsActive { get; set; }
        public string? AssignedBy { get; set; } // ID người phân công
        public DateTime? CreatedAt { get; set; }
    }

    // 4. DTO cho Filter tìm kiếm
    public sealed record StaffAssignmentQueryParameters
    {
        public string? SearchTerm { get; set; }
        public string? BuildingId { get; set; }
        public string? UserId { get; set; }
        public bool? IsActive { get; set; } // Lọc đang làm việc hay đã nghỉ
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public sealed record StaffUserDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string StaffCode { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }

    public sealed record StaffAssignmentBuildingDto
    {
        public string BuildingId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string BuildingCode { get; set; } = null!;
    }
}