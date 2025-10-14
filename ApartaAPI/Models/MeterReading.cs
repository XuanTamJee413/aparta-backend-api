using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class MeterReading
{
    public string Id { get; set; } = null!;

    public string? ApartmentId { get; set; }

    public string? MeterId { get; set; }

    public int? PreviousReading { get; set; }

    public int? CurrentReading { get; set; }

    public DateOnly? ReadingDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual Meter? Meter { get; set; }
}
