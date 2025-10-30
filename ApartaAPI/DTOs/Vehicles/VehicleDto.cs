using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Vehicles
{
   
    public sealed record VehicleQueryParameters(
        string? Status,       
        string? SearchTerm, 
        string? SortBy,       
        string? SortOrder    
    );

    
    public sealed record VehicleDto(
        string VehicleId,
        string ApartmentId,
        string VehicleNumber,
        string? Info,
        string Status,
        DateTime? CreatedAt
    );

    
    public sealed record VehicleCreateDto(
        [Required]
        string ApartmentId,
        [Required]
        string VehicleNumber,
        string? Info,
        [Required]
        string Status
    );

   
    public sealed record VehicleUpdateDto(
        string? VehicleNumber,
        string? Info,
        string? Status
    );
}
