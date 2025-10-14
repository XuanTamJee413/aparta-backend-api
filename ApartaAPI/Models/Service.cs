using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Service
{
    public string ServiceId { get; set; } = null!;

    public string? Name { get; set; }

    public double? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
}
