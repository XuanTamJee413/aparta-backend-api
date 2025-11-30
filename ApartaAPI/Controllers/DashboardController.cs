using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApartaDbContext _context;

        public DashboardController(ApartaDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatisticsDto>), 200)]
        public async Task<ActionResult<ApiResponse<DashboardStatisticsDto>>> GetStatistics()
        {
            try
            {
                var statistics = new DashboardStatisticsDto();

                // Tổng số tòa nhà
                statistics.TotalBuildings = await _context.Buildings
                    .Where(b => b.IsActive)
                    .CountAsync();

                // Tổng số căn hộ
                statistics.TotalApartments = await _context.Apartments
                    .CountAsync();

                // Doanh thu tháng hiện tại
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                statistics.MonthlyRevenue = await _context.Invoices
                    .Where(i => i.Status == "paid" 
                        && i.CreatedAt.HasValue
                        && i.CreatedAt.Value.Month == currentMonth 
                        && i.CreatedAt.Value.Year == currentYear)
                    .SumAsync(i => (decimal?)i.Price) ?? 0;

                // Trạng thái căn hộ: Đã Bán vs Chưa bán
                var totalApartments = statistics.TotalApartments;
                
                // Đếm căn hộ có status = "Đã Bán"
                var soldApartments = await _context.Apartments
                    .Where(a => a.Status == "Đã Bán")
                    .CountAsync();
                
                var unsoldApartments = totalApartments - soldApartments;
                
                statistics.OccupancyRate = totalApartments > 0 
                    ? Math.Round((double)soldApartments / totalApartments * 100, 2) 
                    : 0;

                // Doanh thu từ ngày project đầu tiên được tạo
                var firstProjectDate = await _context.Projects
                    .Where(p => p.CreatedAt.HasValue)
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => p.CreatedAt.Value)
                    .FirstOrDefaultAsync();

                DateTime startDate;
                if (firstProjectDate == default(DateTime))
                {
                    // Nếu không có project nào, lấy 6 tháng gần nhất
                    startDate = DateTime.UtcNow.AddMonths(-5);
                }
                else
                {
                    startDate = firstProjectDate;
                }

                var revenueDataRaw = await _context.Invoices
                    .Where(i => i.Status == "paid" 
                        && i.CreatedAt.HasValue
                        && i.CreatedAt.Value >= startDate)
                    .Select(i => new 
                    { 
                        Year = i.CreatedAt.Value.Year, 
                        Month = i.CreatedAt.Value.Month,
                        Price = i.Price
                    })
                    .ToListAsync();
                
                var revenueData = revenueDataRaw
                    .GroupBy(i => new { i.Year, i.Month })
                    .Select(g => new MonthlyRevenueDto
                    {
                        Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                        Revenue = g.Sum(i => i.Price)
                    })
                    .OrderBy(r => r.Month)
                    .ToList();

                // Đảm bảo có đủ tất cả các tháng từ startDate đến hiện tại
                var months = new List<string>();
                var currentDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
                
                while (startMonth <= currentDate)
                {
                    months.Add($"{startMonth.Month:D2}/{startMonth.Year}");
                    startMonth = startMonth.AddMonths(1);
                }

                foreach (var month in months)
                {
                    if (!revenueData.Any(r => r.Month == month))
                    {
                        revenueData.Add(new MonthlyRevenueDto { Month = month, Revenue = 0 });
                    }
                }

                statistics.RevenueByMonth = revenueData.OrderBy(r => r.Month).ToList();

                // Trạng thái căn hộ
                statistics.ApartmentStatus.Occupied = soldApartments;
                statistics.ApartmentStatus.Vacant = unsoldApartments;

                // Chỉ số chưa ghi: Đếm số căn hộ "Đã Bán" chưa có meter reading cho tháng hiện tại
                var currentBillingPeriod = DateTime.UtcNow.ToString("yyyy-MM");
                var soldApartmentIds = await _context.Apartments
                    .Where(a => a.Status == "Đã Bán")
                    .Select(a => a.ApartmentId)
                    .ToListAsync();

                // Lấy danh sách căn hộ đã có meter reading cho tháng hiện tại
                var apartmentsWithReadings = await _context.MeterReadings
                    .Where(mr => soldApartmentIds.Contains(mr.ApartmentId) 
                        && mr.BillingPeriod == currentBillingPeriod)
                    .Select(mr => mr.ApartmentId)
                    .Distinct()
                    .ToListAsync();

                // Số căn hộ chưa ghi chỉ số = Tổng căn hộ đã bán - Căn hộ đã có reading
                statistics.PendingMeterReadings = soldApartmentIds.Count - apartmentsWithReadings.Count;

                // Hóa đơn chưa thanh toán (placeholder - có thể implement sau)
                statistics.UnpaidInvoices = await _context.Invoices
                    .Where(i => i.Status != "paid")
                    .CountAsync();

                return Ok(ApiResponse<DashboardStatisticsDto>.Success(statistics, "Lấy thống kê dashboard thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DashboardStatisticsDto>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }

        // GET: api/Dashboard/apartment-status-by-project
        [HttpGet("apartment-status-by-project")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectApartmentStatusDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ProjectApartmentStatusDto>>>> GetApartmentStatusByProject()
        {
            try
            {
                var projects = await _context.Projects
                    .Where(p => p.IsActive)
                    .Include(p => p.Buildings)
                        .ThenInclude(b => b.Apartments)
                    .ToListAsync();

                var result = new List<ProjectApartmentStatusDto>();

                foreach (var project in projects)
                {
                    var allApartments = project.Buildings
                        .Where(b => b.IsActive)
                        .SelectMany(b => b.Apartments)
                        .ToList();

                    var totalApartments = allApartments.Count;
                    var soldApartments = allApartments.Count(a => a.Status == "Đã Bán");
                    var unsoldApartments = totalApartments - soldApartments;

                    result.Add(new ProjectApartmentStatusDto
                    {
                        ProjectId = project.ProjectId,
                        ProjectName = project.Name ?? project.ProjectCode ?? "N/A",
                        TotalApartments = totalApartments,
                        SoldApartments = soldApartments,
                        UnsoldApartments = unsoldApartments
                    });
                }

                return Ok(ApiResponse<List<ProjectApartmentStatusDto>>.Success(result, "Lấy thống kê căn hộ theo dự án thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProjectApartmentStatusDto>>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }

        // GET: api/Dashboard/revenue-by-project?year=2024
        [HttpGet("revenue-by-project")]
        [Authorize(Roles = "admin,manager")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectRevenueDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ProjectRevenueDto>>>> GetRevenueByProject([FromQuery] int? year = null)
        {
            try
            {
                if (!year.HasValue)
                {
                    year = DateTime.UtcNow.Year;
                }

                var projects = await _context.Projects
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var result = new List<ProjectRevenueDto>();

                foreach (var project in projects)
                {
                    var projectBuildings = await _context.Buildings
                        .Where(b => b.ProjectId == project.ProjectId && b.IsActive)
                        .Select(b => b.BuildingId)
                        .ToListAsync();

                    var projectApartments = await _context.Apartments
                        .Where(a => projectBuildings.Contains(a.BuildingId))
                        .Select(a => a.ApartmentId)
                        .ToListAsync();

                    var revenueByMonth = new List<MonthlyRevenueDto>();
                    
                    // Lấy doanh thu theo từng tháng trong năm
                    for (int month = 1; month <= 12; month++)
                    {
                        var monthRevenue = await _context.Invoices
                            .Where(i => i.Status == "paid"
                                && i.CreatedAt.HasValue
                                && i.CreatedAt.Value.Year == year.Value
                                && i.CreatedAt.Value.Month == month
                                && projectApartments.Contains(i.ApartmentId))
                            .SumAsync(i => (decimal?)i.Price) ?? 0;

                        revenueByMonth.Add(new MonthlyRevenueDto
                        {
                            Month = $"{month:D2}/{year}",
                            Revenue = monthRevenue
                        });
                    }

                    var totalRevenue = revenueByMonth.Sum(r => r.Revenue);

                    result.Add(new ProjectRevenueDto
                    {
                        ProjectId = project.ProjectId,
                        ProjectName = project.Name ?? project.ProjectCode ?? "N/A",
                        RevenueByMonth = revenueByMonth,
                        TotalRevenue = totalRevenue
                    });
                }

                return Ok(ApiResponse<List<ProjectRevenueDto>>.Success(result, "Lấy doanh thu theo dự án thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ProjectRevenueDto>>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }
    }
}

