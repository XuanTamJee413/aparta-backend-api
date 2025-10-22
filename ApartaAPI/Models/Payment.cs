using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public string InvoiceId { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
