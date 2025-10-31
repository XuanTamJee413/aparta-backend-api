using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class MeterReading
{
    public string MeterReadingId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string MeterId { get; set; } = null!;

    public int PreviousReading { get; set; }

    public int CurrentReading { get; set; }

    public DateOnly ReadingDate { get; set; }

    public string? BillingPeriod { get; set; }

    public string? RecordedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsInvoiced { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual Meter Meter { get; set; } = null!;

    public virtual User? RecordedByUser { get; set; }
}
