using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Building
{
    public string BuildingId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string BuildingCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? NumResidents { get; set; }

    public int? NumApartments { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<PriceQuotation> PriceQuotations { get; set; } = new List<PriceQuotation>();

    public virtual Project Project { get; set; } = null!;
}
