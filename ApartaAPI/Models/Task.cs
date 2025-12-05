using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Task
{
    public string TaskId { get; set; } = null!;

    public string? ServiceBookingId { get; set; }

    public string OperationStaffId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

	public string? AssigneeNote { get; set; }
	public string? VerifyNote { get; set; }

	public virtual User OperationStaff { get; set; } = null!;

    public virtual ServiceBooking? ServiceBooking { get; set; }

    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
