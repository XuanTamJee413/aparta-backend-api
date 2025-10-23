using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class FeePeriod
{
    public string FeePeriodId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string? Items { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
