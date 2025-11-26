using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class UtilityBooking
{
    public string UtilityBookingId { get; set; } = null!;

    public string UtilityId { get; set; } = null!;

    public string ResidentId { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public DateTime? BookedAt { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ResidentNote { get; set; }

    public string? StaffNote { get; set; }

    public virtual User Resident { get; set; } = null!;

    public virtual Utility Utility { get; set; } = null!;
}
