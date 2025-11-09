using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class InvoiceItem
{
    public string InvoiceItemId { get; set; } = null!;

    public string InvoiceId { get; set; } = null!;

    public string FeeType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
