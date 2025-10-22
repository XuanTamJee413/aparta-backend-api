using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class PriceQuotation
{
    public string PriceQuotationId { get; set; } = null!;

    public string BuildingId { get; set; } = null!;

    public string FeeType { get; set; } = null!;

    public string CalculationMethod { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public string? Unit { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Building Building { get; set; } = null!;
}
