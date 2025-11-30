using System;
using System.ComponentModel.DataAnnotations;

namespace ApartaAPI.DTOs.Buildings
{
    // 1. Param
    public sealed record BuildingQueryParameters(
        string? SearchTerm,
        int Skip = 0,
        int Take = 10
    );

    // 2. Output
    public sealed record BuildingDto(
        string BuildingId,
        string ProjectId,
        string? BuildingCode,
        string? Name,

        // Các trường đếm
        int NumApartments,
        int NumResidents,

        // Thông tin vật lý
        int TotalFloors,
        int TotalBasements,
        double? TotalArea,

        // Thông tin khác
        DateOnly? HandoverDate,
        string? WarrantyStatus,
        string? ReceptionPhone,
        string? Description,

        // Cấu hình chốt số
        int ReadingWindowStart,
        int ReadingWindowEnd,

        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    // 3. Create Input
    public sealed record BuildingCreateDto(
        [Required] string ProjectId,
        [Required] string BuildingCode,
        [Required] string Name,
        [Required] int TotalFloors,

        double? TotalArea,
        DateOnly? HandoverDate,
        string? Description,
        string? ReceptionPhone,

        int TotalBasements = 0,
        int ReadingWindowStart = 1,
        int ReadingWindowEnd = 5
    );

    // 4. Update Input
    public sealed record BuildingUpdateDto(
        string? Name,
        bool? IsActive,

        int? TotalFloors,
        int? TotalBasements,

        double? TotalArea,
        DateOnly? HandoverDate,
        string? Description,
        string? ReceptionPhone,

        int? ReadingWindowStart,
        int? ReadingWindowEnd
    );
}