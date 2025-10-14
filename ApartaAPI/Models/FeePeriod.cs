using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class FeePeriod
{
    public string Id { get; set; } = null!;

    public string? ApartmentId { get; set; }

    public string? Items { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual ICollection<UnitPrice> UnitPrices { get; set; } = new List<UnitPrice>();
}
