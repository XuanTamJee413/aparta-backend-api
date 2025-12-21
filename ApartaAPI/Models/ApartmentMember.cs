using System;
using System.Collections.Generic;

namespace ApartaAPI.Models;

public partial class ApartmentMember
{
    public string ApartmentMemberId { get; set; } = null!;

    public string ApartmentId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? FaceImageUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Info { get; set; }

    public string? IdNumber { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool IsOwner { get; set; }

    public string? Nationality { get; set; }

    public string? FamilyRole { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UserId { get; set; }

    public string? RoleId { get; set; }

    public string? HeadMemberId { get; set; }

    public bool IsAppAccess { get; set; }

    public string? TemporaryRegistrationCode { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ApartmentMember? HeadMember { get; set; }

    public virtual ICollection<ApartmentMember> InverseHeadMember { get; set; } = new List<ApartmentMember>();

    public virtual Role? Role { get; set; }

    public virtual User? User { get; set; }
}
