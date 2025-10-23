using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class VisitLog
{
    public string VisitLogId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string VisitorId { get; set; } = null!;

    public DateTime CheckinTime { get; set; }

    public DateTime? CheckoutTime { get; set; }

    public string? Purpose { get; set; }

    public string Status { get; set; } = null!;

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual Visitor Visitor { get; set; } = null!;
}
