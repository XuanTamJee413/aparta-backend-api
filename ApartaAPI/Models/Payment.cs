using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public double? Amount { get; set; }

    public string? Type { get; set; }

    public string? ReceiptId { get; set; }

    public string? SubscriptionId { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Receipt? Receipt { get; set; }

    public virtual Subscription? Subscription { get; set; }
}
