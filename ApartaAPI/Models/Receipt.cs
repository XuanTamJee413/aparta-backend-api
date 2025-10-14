using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Receipt
{
    public string ReceiptId { get; set; } = null!;

    public double? Price { get; set; }

    public string? Type { get; set; }

    public int? Quantity { get; set; }

    public double? TotalAmount { get; set; }

    public string? ApartmentId { get; set; }

    public double? Tax { get; set; }

    public double? Discount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
