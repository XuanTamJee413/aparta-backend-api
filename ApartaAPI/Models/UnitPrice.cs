using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class UnitPrice
{
    public string UnitPriceId { get; set; } = null!;

    public string? FeePeriodId { get; set; }

    public string? FeeType { get; set; }

    public string? CalculationMethod { get; set; }

    public decimal? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FeePeriod? FeePeriod { get; set; }
}
