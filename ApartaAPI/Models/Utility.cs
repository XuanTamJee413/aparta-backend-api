using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Utility
{
    public string UtilityId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Location { get; set; }

    public double? PeriodTime { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
	public string? BuildingId { get; set; }

	public virtual Building Building { get; set; } = null!;

	public virtual ICollection<UtilityBooking> UtilityBookings { get; set; } = new List<UtilityBooking>();
}
