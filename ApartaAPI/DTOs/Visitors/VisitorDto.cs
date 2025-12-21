using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Visitors
{
    public class VisitorDto
    {
        public string VisitorId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        public bool IsUpdated { get; set; } = false;
    }
    public class VisitorCreateDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(255, ErrorMessage = "Họ tên không quá 255 ký tự")]
        [RegularExpression(@"^[\p{L}0-9\s]+$", ErrorMessage = "Họ tên không được chứa ký tự đặc biệt")]
        public string FullName { get; set; } = null!;

        [RegularExpression(@"^[0-9]{10,13}$", ErrorMessage = "Số điện thoại phải từ 10 đến 13 số")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "CCCD/Hộ chiếu là bắt buộc")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "CCCD/Hộ chiếu chỉ được chứa số")]
        [StringLength(20, ErrorMessage = "CCCD/Hộ chiếu không quá 20 ký tự")]
        public string IdNumber { get; set; } = null!;

        [Required(ErrorMessage = "Mã căn hộ là bắt buộc")]
        public string ApartmentId { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mục đích không quá 500 ký tự")]
        [RegularExpression(@"^[\p{L}0-9\s,.-]+$", ErrorMessage = "Mục đích không được chứa ký tự đặc biệt nguy hiểm")]
        public string? Purpose { get; set; }

        public string? CheckinTime { get; set; }
        public string? Status { get; set; }
    }
    public class VisitorCheckResponseDto
    {
        public bool Exists { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }
}