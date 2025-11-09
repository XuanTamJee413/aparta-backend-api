using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class ServiceBooking
{
    public string ServiceBookingId { get; set; } = null!;

    public string ServiceId { get; set; } = null!;

    public string ResidentId { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal? PaymentAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ResidentNote { get; set; }

    public string? StaffNote { get; set; }

    public virtual User Resident { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
