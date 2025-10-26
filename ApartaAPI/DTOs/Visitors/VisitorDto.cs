using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Visitors
{
    // visitor fang khong bi vong lap vs víitlog
    public class VisitorDto
    {
        public string VisitorId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
    }
    public class VisitorCreateDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        public string FullName { get; set; } = null!;

        [StringLength(15, ErrorMessage = "Số điện thoại không quá 15 ký tự")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "CCCD/Hộ chiếu là bắt buộc")]
        [StringLength(20, ErrorMessage = "CCCD/Hộ chiếu không quá 20 ký tự")]
        public string IdNumber { get; set; } = null!;

        [Required(ErrorMessage = "Mã căn hộ là bắt buộc")]
        public string ApartmentId { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mục đích không quá 500 ký tự")]
        public string? Purpose { get; set; }

        public string? CheckinTime { get; set; }
        public string? Status { get; set; }
    }
}