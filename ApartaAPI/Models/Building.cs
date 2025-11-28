using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Building
{
    public string BuildingId { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string BuildingCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public int ReadingWindowStart { get; set; }

    public int ReadingWindowEnd { get; set; }

    public int TotalFloors { get; set; }

    public int TotalBasements { get; set; }

    public double? TotalArea { get; set; }

    public DateOnly? HandoverDate { get; set; }

    public string? ReceptionPhone { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();

    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<PriceQuotation> PriceQuotations { get; set; } = new List<PriceQuotation>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<StaffBuildingAssignment> StaffBuildingAssignments { get; set; } = new List<StaffBuildingAssignment>();
}
