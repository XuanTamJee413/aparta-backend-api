using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class TaskAssignment
{
    public string TaskAssignmentId { get; set; } = null!;

    public string? TaskId { get; set; }

    public string? ManagementStaffId { get; set; }

    public string? ServiceStaffId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ManagementStaff { get; set; }

    public virtual User? ServiceStaff { get; set; }

    public virtual Task? Task { get; set; }
}
