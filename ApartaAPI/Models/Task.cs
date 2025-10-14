using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Task
{
    public string TaskId { get; set; } = null!;

    public string? Type { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? ServiceBookingId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ServiceBooking? ServiceBooking { get; set; }

    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
