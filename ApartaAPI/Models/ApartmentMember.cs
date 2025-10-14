using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class ApartmentMember
{
    public string ApartmentMemberId { get; set; } = null!;

    public string? ApartmentId { get; set; }

    public string? Name { get; set; }

    public string? FaceImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Info { get; set; }

    public string? IdNumber { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? IsOwned { get; set; }

    public string? Nationality { get; set; }

    public string? FamilyRole { get; set; }

    public virtual Apartment? Apartment { get; set; }
}
