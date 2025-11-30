namespace ApartaAPI.DTOs.User
{
    public class UserAccountDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? StaffCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsDeleted { get; set; } // Thêm trường này để quản lý trạng thái xóa

        // Chi tiết Resident
        public string? ApartmentCode { get; set; }

        // Chi tiết Staff
        public List<string> AssignedBuildingCodes { get; set; } = new List<string>();
    }
    public class UserDetailDto
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string RoleId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public List<string> AssignedBuildingIds { get; set; } = new List<string>(); // Dành cho Staff
    }
    public class UserQueryParams
    {
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
            }
        }

        public string? SearchTerm { get; set; }

        public string? Status { get; set; }

        public string? RoleName { get; set; }

        public string? SortColumn { get; set; }

        public string? SortDirection { get; set; }
    }
}
