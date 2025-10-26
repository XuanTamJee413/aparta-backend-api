using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.ApartmentMembers
{
    public sealed record ApartmentMemberQueryParameters(
        bool? IsOwned,
        string? SearchTerm,
        string? SortBy,
        string? SortOrder
    );

    public class ApartmentMemberDto
    {
        public string ApartmentMemberId { get; set; }
        public string? ApartmentId { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        public bool IsOwner { get; set; }

        public string? FaceImageUrl { get; set; }
        public string? Info { get; set; }
        public string? Nationality { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? FamilyRole { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }
    }

    public class ApartmentMemberCreateDto
    {
        [Required]
        public string ApartmentId { get; set; }

        [Required]
        public string Name { get; set; }

        public string? PhoneNumber { get; set; }
        public string? IdNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool? IsOwner { get; set; }
        public string? FamilyRole { get; set; }
        public string? FaceImageUrl { get; set; }
        public string? Info { get; set; }
        public string? Nationality { get; set; }
        public string? Status { get; set; }
    }

    public class ApartmentMemberUpdateDto
    {

        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IdNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool? IsOwner { get; set; }
        public string? FamilyRole { get; set; }
        public string? FaceImageUrl { get; set; }
        public string? Info { get; set; }
        public string? Nationality { get; set; }
        public string? Status { get; set; }
    }
}
