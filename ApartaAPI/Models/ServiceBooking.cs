using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class ServiceBooking
{
    public string ServiceBookingId { get; set; } = null!;

    public string? ServiceId { get; set; }

    public string? ResidentId { get; set; }

    public DateOnly? BookingDate { get; set; }

    public string? Status { get; set; }

    public decimal? PaymentAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Resident { get; set; }

    public virtual Service? Service { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
