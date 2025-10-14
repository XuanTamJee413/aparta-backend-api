using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? ApartmentId { get; set; }

    public string RoleId { get; set; } = null!;

    public string? PermissionId { get; set; }

    public string? StaffCode { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<News> News { get; set; } = new List<News>();

    public virtual PermissionGroup? Permission { get; set; }

    public virtual ICollection<Propose> Proposes { get; set; } = new List<Propose>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<TaskAssignment> TaskAssignmentManagementStaffs { get; set; } = new List<TaskAssignment>();

    public virtual ICollection<TaskAssignment> TaskAssignmentServiceStaffs { get; set; } = new List<TaskAssignment>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<UtilityBooking> UtilityBookings { get; set; } = new List<UtilityBooking>();
}
