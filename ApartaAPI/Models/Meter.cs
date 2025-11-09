using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Meter
{
    public string MeterId { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
