using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Vehicle
{
    public string VehicleId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string VehicleNumber { get; set; } = null!;

    public string? Info { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;
}
