using ApartaAPI.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Tasks
{
    // 1. DTO để tạo Task mới
    public sealed record TaskCreateDto(
        string? ServiceBookingId, 
        [Required] string Type,   
        [Required] string Description,
        DateTime? StartDate,
        DateTime? EndDate
    );

    // 2. DTO để phân công (Assign)
    public sealed record TaskAssignmentCreateDto(
        [Required] string TaskId,
        [Required] string AssigneeUserId 
    );

    // 3. DTO để cập nhật trạng thái (cho Maintenance Staff)
    public sealed record TaskUpdateStatusDto(
        [Required] string Status,
        string? Note 
    );

    // 4. DTO hiển thị Task (Output)
    public sealed record TaskDto(
        string TaskId,
        string? ServiceBookingId,
        string OperationStaffId,
        string OperationStaffName, 
        string Type,
        string Description,
        string Status,
        DateTime? StartDate,
        DateTime? EndDate,
        DateTime? CreatedAt,
        string? AssigneeUserId, 
        string? AssigneeName,
        DateTime? AssignedDate,
		string? AssigneeNote
	);

}