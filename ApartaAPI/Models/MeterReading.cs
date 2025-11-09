using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class MeterReading
{
    public string MeterReadingId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string FeeType { get; set; } = null!;

    public decimal ReadingValue { get; set; }

    public DateOnly ReadingDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? RecordedBy { get; set; }

    public string? BillingPeriod { get; set; }

    public string? InvoiceItemId { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual InvoiceItem? InvoiceItem { get; set; }

    public virtual User? RecordedByNavigation { get; set; }
}
