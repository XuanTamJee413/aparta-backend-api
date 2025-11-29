using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Proposals
{
    public class ProposalCreateDto
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = null!;

        // Tùy chọn: Resident có thể chọn Staff (nếu có dropdown chọn Staff)
        // Nếu không có trường này, Backend sẽ tự tìm Staff phụ trách.
        // public string? OperationStaffId { get; set; }
    }
    public class ProposalDto
    {
        public string ProposalId { get; set; } = null!;
        public string ResidentId { get; set; } = null!;
        public string ResidentName { get; set; } = null!; // Join từ User
        public string? OperationStaffId { get; set; }
        public string? OperationStaffName { get; set; } // Join từ User
        public string Content { get; set; } = null!;
        public string? Reply { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class ProposalReplyDto
    {
        [Required]
        [MaxLength(2000)]
        public string ReplyContent { get; set; } = null!;
    }
    public class ProposalQueryParams
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortColumn { get; set; } // Hỗ trợ: CreatedAt, Status
        public string? SortDirection { get; set; } = "desc";
    }
}
