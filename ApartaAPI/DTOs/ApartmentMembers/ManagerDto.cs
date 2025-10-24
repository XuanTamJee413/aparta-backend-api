using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.ApartmentMembers
{
    public class ManagerDto
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? StaffCode { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? PermissionGroup { get; set; }
    }

    public sealed record CreateManagerDto
    {
        //EF3: Validation for creating manager
        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự")]
        public string Name { get; init; } = null!;
        //Ef3: Phone number validation
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        public string Phone { get; init; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự")]
        public string Password { get; init; } = null!;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        public string? Email { get; init; }

        [StringLength(50, ErrorMessage = "Mã nhân viên không được vượt quá 50 ký tự")]
        public string? StaffCode { get; init; }

        public string? AvatarUrl { get; init; }
    }

    public sealed record UpdateManagerDto
    {
        //ef2: Validation for updating manager
        [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự")]
        public string? Name { get; init; }

        //ef1: Phone number validation
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        public string? Phone { get; init; }

        //ef2: Password validation

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự")]
        public string? Password { get; init; }

        //ef1: Email validation

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        public string? Email { get; init; }

        //ef2: Staff code validation
        [StringLength(50, ErrorMessage = "Mã nhân viên không được vượt quá 50 ký tự")]
        public string? StaffCode { get; init; }

        public string? AvatarUrl { get; init; }
    }
}