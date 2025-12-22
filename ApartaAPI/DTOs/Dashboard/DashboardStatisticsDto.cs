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
        public int PendingMeterReadings { get; set; }
        public int UnpaidInvoices { get; set; }
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

    public class ProjectApartmentStatusDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int TotalApartments { get; set; }
        public int SoldApartments { get; set; }
        public int UnsoldApartments { get; set; }
    }

    public class ProjectRevenueDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = new List<MonthlyRevenueDto>();
        public decimal TotalRevenue { get; set; }
    }

    // Building level
    public class BuildingApartmentStatusDto
    {
        public string BuildingId { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public int TotalApartments { get; set; }
        public int SoldApartments { get; set; }
        public int UnsoldApartments { get; set; }
    }

    public class BuildingRevenueDto
    {
        public string BuildingId { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = new List<MonthlyRevenueDto>();
        public decimal TotalRevenue { get; set; }
    }
}

