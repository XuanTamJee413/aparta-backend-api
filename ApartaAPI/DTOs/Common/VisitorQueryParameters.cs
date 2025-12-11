namespace ApartaAPI.DTOs.Common
{
    public class VisitorQueryParameters
    {
        private const int MaxPageSize = 50;

        public string? ApartmentId { get; set; } 
        public string? SearchTerm { get; set; }
        public string? BuildingId { get; set; }

        public string? SortColumn { get; set; } 
        public string? SortDirection { get; set; } = "desc"; 

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
