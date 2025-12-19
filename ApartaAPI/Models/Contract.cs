using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class Contract
{
    public string ContractId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string? Image { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string ContractNumber { get; set; } = null!;

    public string ContractType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal? DepositAmount { get; set; }

    public decimal? TotalValue { get; set; }

    public string? RepresentativeMemberId { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual ApartmentMember? RepresentativeMember { get; set; }
}
