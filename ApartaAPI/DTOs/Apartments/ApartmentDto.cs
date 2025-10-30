using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Apartments
{
   
    public sealed record ApartmentQueryParameters(
        string? BuildingId,   
        string? Status,       
        string? SearchTerm,   
        string? SortBy,       
        string? SortOrder    
    );

   
    public sealed record ApartmentDto(
        string ApartmentId,
        string BuildingId,
        string Code,
        string? Type,
        string Status,
        double? Area,
        DateTime? CreatedAt
    );

    public sealed record ApartmentCreateDto(
        [Required]
        string BuildingId,
        [Required]
        string Code,
        string? Type,
        [Required]
        string Status,
        double? Area
    );

    
    public sealed record ApartmentUpdateDto(
        string? Code,
        string? Type,
        string? Status,
        double? Area
    
    );
}
