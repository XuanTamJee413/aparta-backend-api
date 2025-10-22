using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class UtilityBooking
{
    public string UtilityBookingId { get; set; } = null!;

    public string? UtilityId { get; set; }

    public string? ResidentId { get; set; }

    public DateOnly? BookingDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Resident { get; set; }

    public virtual Utility? Utility { get; set; }
}
