namespace ApartaAPI.DTOs.Projects
{
    public sealed record ProjectQueryParameters(
        bool? IsActive,
        string? SearchTerm, // Dùng cho Project Name và Project Code
        string? SortBy,     // "numApartments" hoặc "numBuildings"
        string? SortOrder   // "asc" hoặc "desc"
    );

    // Read model
    public sealed record ProjectDto(
        string ProjectId,
        string? ProjectCode,
        string? Name,
        int? NumApartments,
        int? NumBuildings,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive
    );

    // Create input
    public sealed record ProjectCreateDto(
        string? ProjectId,
        string? ProjectCode,
        string? Name,
        int? NumApartments,
        int? NumBuildings
    );

    // Update input
    public sealed record ProjectUpdateDto(
        string? ProjectCode, // Sẽ bị bỏ qua bởi AutoMapper (BR-19)
        string? Name,
        int? NumApartments,
        int? NumBuildings,
        bool? IsActive
    );
}