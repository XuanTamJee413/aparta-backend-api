namespace ApartaAPI.DTOs.Dashboard
{
    public class DashboardStatisticsDto
    {
        public int TotalBuildings { get; set; }
        public int TotalApartments { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double OccupancyRate { get; set; }
        public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = new List<MonthlyRevenueDto>();
        public ApartmentStatusDto ApartmentStatus { get; set; } = new ApartmentStatusDto();
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class ApartmentStatusDto
    {
        public int Occupied { get; set; }
        public int Vacant { get; set; }
    }
}

