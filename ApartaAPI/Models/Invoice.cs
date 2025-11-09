using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Invoice
{
    public string InvoiceId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string? StaffId { get; set; }

    public string FeeType { get; set; } = null!;

    public decimal Price { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User? Staff { get; set; }
}
