namespace ApartaAPI.DTOs.Common
{
    public sealed record PaginatedResult<T>(
        IEnumerable<T> Items,
        int TotalCount
    );
}
