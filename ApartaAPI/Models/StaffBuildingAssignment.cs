using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class StaffBuildingAssignment
{
    public string UserId { get; set; } = null!;

    public string BuildingId { get; set; } = null!;

    public DateOnly AssignmentStartDate { get; set; }

    public DateOnly? AssignmentEndDate { get; set; }

    public string? ScopeOfWork { get; set; }

    public bool IsActive { get; set; }

    public virtual Building Building { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
