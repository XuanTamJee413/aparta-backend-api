using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Asset
{
    public string AssetId { get; set; } = null!;

    public string BuildingId { get; set; } = null!;

    public string? Info { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Building Building { get; set; } = null!;
}
