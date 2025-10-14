using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Vehicle
{
    public string VehicleId { get; set; } = null!;

    public string? Info { get; set; }

    public string? ApartmentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? VehivleNumber { get; set; }

    public virtual Apartment? Apartment { get; set; }
}
