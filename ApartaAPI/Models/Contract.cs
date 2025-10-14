using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Contract
{
    public string ContactId { get; set; } = null!;

    public string? Image { get; set; }

    public string? ApartmentId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment? Apartment { get; set; }
}
