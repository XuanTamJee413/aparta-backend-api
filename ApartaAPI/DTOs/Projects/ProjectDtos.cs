namespace ApartaAPI.DTOs.Projects
{
    // Read model
    public sealed record ProjectDto(
        string ProjectId,
        string? ProjectCode,
        string? Name,
        int? NumApartments,
        int? NumBuildings,
        DateTime? CreatedAt,
        DateTime? UpdatedAt
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
        string? ProjectCode,
        string? Name,
        int? NumApartments,
        int? NumBuildings
    );
}