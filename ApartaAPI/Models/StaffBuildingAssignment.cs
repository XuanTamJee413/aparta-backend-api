using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class StaffBuildingAssignment
{
    public string AssignmentId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string BuildingId { get; set; } = null!;

    public DateOnly AssignmentStartDate { get; set; }

    public DateOnly? AssignmentEndDate { get; set; }

    public string? ScopeOfWork { get; set; }

    public bool IsActive { get; set; }

    public string? Position { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedByNavigation { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
