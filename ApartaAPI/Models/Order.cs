using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Order
{
    public string OrderId { get; set; } = null!;

    public string? Status { get; set; }

    public string? PaymentInfo { get; set; }

    public string? ProjectId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public double? TotalAmount { get; set; }

    public string? OrderCode { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public int? NumMonths { get; set; }

    public double? Tax { get; set; }

    public double? Discount { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Project? Project { get; set; }
}
