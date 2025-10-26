using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Subscriptions
{
    /// <summary>
    /// (UC 2.1.1) Tham số truy vấn cho danh sách Subscriptions.
    /// Hỗ trợ tìm kiếm (theo ngày tạo), lọc (Status), và phân trang.
    /// </summary>
    public sealed record SubscriptionQueryParameters(
        DateTime? CreatedAtStart, // Tìm theo ngày tạo (từ ngày)
        DateTime? CreatedAtEnd,   // Tìm theo ngày tạo (đến ngày)
        string? Status,           // Lọc theo trạng thái (e.g., "Draft", "Active", "Expired")
        int Skip = 0,
        int Take = 10
    );

    /// <summary>
    /// (UC 2.1.1) DTO hiển thị thông tin Subscription.
    /// </summary>
    public sealed record SubscriptionDto(
        string SubscriptionId,
        string ProjectId,
        string SubscriptionCode, // Mã gói (có thể tự sinh hoặc nhập)
        string Status, // "Draft", "Active", "Expired"
        decimal Amount, // Giá gốc của gói theo số tháng
        int NumMonths, // Số tháng của lần gia hạn này
        DateTime ExpiredAt, // Ngày hết hạn (chỉ có ý nghĩa khi Status="Active")
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        // (UC 2.1.2) Các trường ghi lại thông tin thanh toán thủ công
        decimal? AmountPaid,
        DateTime? PaymentDate,
        string? PaymentMethod,
        string? PaymentNote
    );

    /// <summary>
    /// (UC 2.1.2, UC 2.1.3) DTO đầu vào cho việc Tạo hoặc Cập nhật bản ghi gia hạn.
    /// </summary>
    public sealed record SubscriptionCreateOrUpdateDto(
        [Required(ErrorMessage = "SM02")] // Yêu cầu ProjectId khi tạo
        string ProjectId,

        [Required(ErrorMessage = "SM02")] // Yêu cầu mã gói
        string SubscriptionCode,

        [Required(ErrorMessage = "SM02")]
        [Range(1, int.MaxValue, ErrorMessage = "SM02")] // Số tháng phải > 0
        int NumMonths,

        [Required(ErrorMessage = "SM02")]
        [Range(0, double.MaxValue, ErrorMessage = "SM02")] // Giá gốc >= 0
        decimal Amount,

        // --- Thông tin thanh toán (có thể null khi lưu nháp lần đầu) ---
        [Range(0, double.MaxValue, ErrorMessage = "SM02")]
        decimal? AmountPaid,

        string? PaymentMethod,
        DateTime? PaymentDate,
        string? PaymentNote,

        // --- Cờ quyết định hành động ---
        [Required]
        bool IsApproved // true = Approve (Lưu), false = Save Draft (Lưu Nháp)
    );
}