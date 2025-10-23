using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Subscription
{
    public string SubscriptionId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string SubscriptionCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? AmountPaid { get; set; }

    public string? PaymentNote { get; set; }

    public decimal Amount { get; set; }

    public int NumMonths { get; set; }

    public double? Tax { get; set; }

    public double? Discount { get; set; }

    public DateTime ExpiredAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
