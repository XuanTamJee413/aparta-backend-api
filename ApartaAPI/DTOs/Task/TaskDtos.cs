using ApartaAPI.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Tasks
{
    // 1. DTO để tạo Task mới
    public sealed record TaskCreateDto(
        string? ServiceBookingId, // Có thể null nếu tạo task lẻ
        [Required] string Type,   // Ví dụ: "Repair", "Cleaning", "Inspection"
        [Required] string Description,
        DateTime? StartDate,
        DateTime? EndDate
    );

    // 2. DTO để phân công (Assign)
    public sealed record TaskAssignmentCreateDto(
        [Required] string TaskId,
        [Required] string AssigneeUserId // ID của Maintenance Staff
    );

    // 3. DTO để cập nhật trạng thái (cho Maintenance Staff)
    public sealed record TaskUpdateStatusDto(
        [Required] string Status, // "In Progress", "Completed", "Cancelled"
        string? Note // Ghi chú khi hoàn thành
    );

    // 4. DTO hiển thị Task (Output)
    public sealed record TaskDto(
        string TaskId,
        string? ServiceBookingId,
        string OperationStaffId,
        string OperationStaffName, // Tên người tạo
        string Type,
        string Description,
        string Status,
        DateTime? StartDate,
        DateTime? EndDate,
        DateTime? CreatedAt,
        // Thông tin người được giao việc (Lấy từ TaskAssignment mới nhất)
        string? AssigneeUserId, 
        string? AssigneeName,
        DateTime? AssignedDate
    );

}