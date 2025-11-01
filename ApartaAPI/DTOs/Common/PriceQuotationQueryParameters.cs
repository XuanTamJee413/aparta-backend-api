namespace ApartaAPI.DTOs.Common
{
    public class PriceQuotationQueryParameters
    {
        public string? BuildingId { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; } = "desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
