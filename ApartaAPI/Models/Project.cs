using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Project
{
    public string ProjectId { get; set; } = null!;

    public string? ProjectCode { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? NumApartments { get; set; }

    public int? NumBuildings { get; set; }

    public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
