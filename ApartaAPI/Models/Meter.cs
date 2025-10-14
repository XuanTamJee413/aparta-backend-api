using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Meter
{
    public string MeterId { get; set; } = null!;

    public string? Type { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
