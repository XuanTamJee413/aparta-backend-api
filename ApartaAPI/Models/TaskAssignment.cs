using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class TaskAssignment
{
    public string TaskAssignmentId { get; set; } = null!;

    public string TaskId { get; set; } = null!;

    public string AssignerUserId { get; set; } = null!;

    public string AssigneeUserId { get; set; } = null!;

    public DateTime AssignedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User AssigneeUser { get; set; } = null!;

    public virtual User AssignerUser { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;
}
