namespace ApartaAPI.DTOs.Buildings
{
    /// <summary>
    /// Parameters for querying the building list with pagination and search.
    /// </summary>
    public sealed record BuildingQueryParameters(
        string? SearchTerm, // Used for BuildingCode and Name
        int Skip = 0,       // For pagination: number of records to skip
        int Take = 10       // For pagination: number of records to take (page size)
    );

    /// <summary>
    /// Represents paginated results along with total count.
    /// </summary>
    public sealed record PaginatedResult<T>(
        IEnumerable<T> Items,
        int TotalCount
    );

    // Read model
    public sealed record BuildingDto(
        string BuildingId,
        string ProjectId, // Added ProjectId for context
        string? BuildingCode,
        string? Name,
        int? NumResidents,
        int? NumApartments,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        bool IsActive // Assuming IsActive exists or will be added to the Building model
    );

    // Create input
    public sealed record BuildingCreateDto(
        string ProjectId, // Need ProjectId to associate the building
        string BuildingCode,
        string Name,
        int? NumApartments,
        int? NumResidents
    );

    // Update input
    public sealed record BuildingUpdateDto(
        // BuildingCode is intentionally omitted as per BR-19
        string? Name,
        int? NumApartments,
        int? NumResidents,
        bool? IsActive // Added for soft delete/deactivation
    );
}