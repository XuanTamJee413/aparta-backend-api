using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Receipt
{
    public string ReceiptId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public decimal Price { get; set; }

    public string Type { get; set; } = null!;

    public int? Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public double? Tax { get; set; }

    public double? Discount { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;
}
