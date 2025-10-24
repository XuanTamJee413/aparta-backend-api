using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Project
{
    public string ProjectId { get; set; } = null!;

    public string? AdminId { get; set; }

    public string ProjectCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? NumApartments { get; set; }

    public int? NumBuildings { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual User? Admin { get; set; }

    public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();

    public virtual ICollection<FeePeriod> FeePeriods { get; set; } = new List<FeePeriod>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
