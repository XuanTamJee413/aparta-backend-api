using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class VisitLog
{
    public string VisitLogId { get; set; } = null!;

    public string? ApartmentId { get; set; }

    public string? VisitorId { get; set; }

    public DateTime? CheckinTime { get; set; }

    public DateTime? CheckoutTime { get; set; }

    public string? Purpose { get; set; }

    public string? Status { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual Visitor? Visitor { get; set; }
}
