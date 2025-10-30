namespace ApartaAPI.DTOs.Common
{
	public class ServiceQueryParameters
	{
		private const int MaxPageSize = 50;
		private int _pageSize = 10;

		public int PageNumber { get; set; } = 1;

		public int PageSize
		{
			get => _pageSize;
			set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
		}

		// Dùng để tìm kiếm theo tên
		public string? SearchTerm { get; set; }

		// Dùng để lọc theo Status
		public string? Status { get; set; }
	}
}
