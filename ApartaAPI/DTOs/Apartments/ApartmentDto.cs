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
        int? Floor,
        double? Area,
        DateTime? CreatedAt
    );

    public sealed record ApartmentCreateDto(
        [Required]
        string BuildingId,

        [Required]
        string Code,

        string? Type,

        double? Area,

        [Required]
        int? Floor
    );

    public sealed record ApartmentUpdateDto(
        string? Code,
        string? Type,
        string? Status,
        double? Area,
        int? Floor
    );

    public sealed record ApartmentTemplateDto(
        int RoomIndex,  
        string? Type,
        double? Area
    );

    public sealed record ApartmentBulkCreateDto(
        [Required]
        string BuildingId,

        [Required]
        int StartFloor,

        [Required]
        int EndFloor,

        [Required]
        IReadOnlyList<ApartmentTemplateDto> Rooms
    );
}
