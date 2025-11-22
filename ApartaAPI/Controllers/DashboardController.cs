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
        [Authorize(Roles = "admin")]
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

                // Tỷ lệ lấp đầy (căn hộ đã có hợp đồng còn hiệu lực hoặc status = "occupied")
                var totalApartments = statistics.TotalApartments;
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                
                // Đếm căn hộ có status = "occupied"
                var occupiedByStatus = await _context.Apartments
                    .Where(a => a.Status == "occupied")
                    .Select(a => a.ApartmentId)
                    .ToListAsync();
                
                // Lấy danh sách apartment IDs có hợp đồng còn hiệu lực
                var occupiedByContract = await _context.Contracts
                    .Where(c => c.EndDate.HasValue && c.EndDate.Value >= today)
                    .Select(c => c.ApartmentId)
                    .Distinct()
                    .ToListAsync();
                
                // Gộp 2 danh sách và loại bỏ trùng lặp
                var allOccupiedIds = occupiedByStatus.Union(occupiedByContract).ToList();
                var occupiedApartments = allOccupiedIds.Count;
                
                statistics.OccupancyRate = totalApartments > 0 
                    ? Math.Round((double)occupiedApartments / totalApartments * 100, 2) 
                    : 0;

                // Doanh thu 6 tháng gần nhất
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
                var revenueDataRaw = await _context.Invoices
                    .Where(i => i.Status == "paid" 
                        && i.CreatedAt.HasValue
                        && i.CreatedAt.Value >= sixMonthsAgo)
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

                // Đảm bảo có đủ 6 tháng (nếu thiếu thì thêm tháng với doanh thu 0)
                var months = new List<string>();
                for (int i = 5; i >= 0; i--)
                {
                    var date = DateTime.UtcNow.AddMonths(-i);
                    months.Add($"{date.Month:D2}/{date.Year}");
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
                statistics.ApartmentStatus.Occupied = occupiedApartments;
                statistics.ApartmentStatus.Vacant = totalApartments - occupiedApartments;

                return Ok(ApiResponse<DashboardStatisticsDto>.Success(statistics, "Lấy thống kê dashboard thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DashboardStatisticsDto>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }
    }
}

