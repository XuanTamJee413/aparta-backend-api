using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Roles
{
    // DTO để hiển thị thông tin Role
    public sealed record RoleDto
    {
        public string RoleId { get; init; } = null!;
        public string RoleName { get; init; } = null!;
        public bool IsSystemDefined { get; init; }
        public bool IsActive { get; init; }
    }

    // DTO để tạo Role Custom
    public sealed record RoleCreateDto
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên Role phải từ 3 đến 100 ký tự")]
        public string RoleName { get; init; } = null!;
    }

    // DTO để cập nhật Role Custom
    public sealed record RoleUpdateDto
    {
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên Role phải từ 3 đến 100 ký tự")]
        public string RoleName { get; init; } = null!;
    }

    // DTO để hiển thị Permission
    public sealed record PermissionDto
    {
        public string PermissionId { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string GroupName { get; init; } = null!;
    }

    // DTO để gán Permissions cho Role
    public sealed record PermissionAssignmentDto
    {
        [Required]
        public List<string> PermissionIds { get; init; } = new List<string>();
    }
}