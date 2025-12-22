using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

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
        [ProducesResponseType(typeof(ApiResponse<DashboardStatisticsDto>), 200)]
        public async Task<ActionResult<ApiResponse<DashboardStatisticsDto>>> GetStatistics()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleBuildingIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<DashboardStatisticsDto>.Fail("Không thể xác định người dùng."));
                    }

                    accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();
                }

                var statistics = new DashboardStatisticsDto();

                // Tổng số tòa nhà
                statistics.TotalBuildings = await _context.Buildings
                    .Where(b => b.IsActive && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(b.BuildingId))))
                    .CountAsync();

                // Tổng số căn hộ
                statistics.TotalApartments = await _context.Apartments
                    .Where(a => !isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(a.BuildingId)))
                    .CountAsync();

                // Doanh thu tháng hiện tại
                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                // Filter invoices by manager-accessible apartments if needed
                List<string>? accessibleApartmentIds = null;
                if (isManager && accessibleBuildingIds != null)
                {
                    accessibleApartmentIds = await _context.Apartments
                        .Where(a => accessibleBuildingIds.Contains(a.BuildingId))
                        .Select(a => a.ApartmentId)
                        .ToListAsync();
                }

                var invoicesQuery = _context.Invoices.AsQueryable();
                if (isManager && accessibleApartmentIds != null)
                {
                    invoicesQuery = invoicesQuery.Where(i => accessibleApartmentIds.Contains(i.ApartmentId));
                }

                statistics.MonthlyRevenue = await invoicesQuery
                    .Where(i => i.Status == "paid" 
                        && i.CreatedAt.HasValue
                        && i.CreatedAt.Value.Month == currentMonth 
                        && i.CreatedAt.Value.Year == currentYear)
                    .SumAsync(i => (decimal?)i.Price) ?? 0;

                // Trạng thái căn hộ: Đã Bán vs Chưa bán
                var totalApartments = statistics.TotalApartments;
                
                // Đếm căn hộ có status = "Đã Bán"
                var soldApartments = await _context.Apartments
                    .Where(a => a.Status == "Đã Bán" && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(a.BuildingId))))
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
                        && i.CreatedAt.Value >= startDate
                        && (!isManager || (accessibleApartmentIds != null && accessibleApartmentIds.Contains(i.ApartmentId))))
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
                    .Where(a => a.Status == "Đã Bán" && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(a.BuildingId))))
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
                    .Where(i => i.Status != "paid" && (!isManager || (accessibleApartmentIds != null && accessibleApartmentIds.Contains(i.ApartmentId))))
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
        [ProducesResponseType(typeof(ApiResponse<List<ProjectApartmentStatusDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ProjectApartmentStatusDto>>>> GetApartmentStatusByProject()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleBuildingIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<List<ProjectApartmentStatusDto>>.Fail("Không thể xác định người dùng."));
                    }

                    accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();
                }

                var projects = await _context.Projects
                    .Where(p => p.IsActive)
                    .Include(p => p.Buildings)
                        .ThenInclude(b => b.Apartments)
                    .ToListAsync();

                var result = new List<ProjectApartmentStatusDto>();

                foreach (var project in projects)
                {
                    // Lọc buildings thuộc quyền manager (nếu là manager)
                    var accessibleBuildings = project.Buildings
                        .Where(b => b.IsActive && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(b.BuildingId))))
                        .ToList();

                    // Manager: nếu project không có building nào thuộc quyền => không trả về project này
                    if (isManager && accessibleBuildings.Count == 0)
                    {
                        continue;
                    }

                    // Lấy tất cả apartments từ các buildings thuộc quyền
                    var allApartments = accessibleBuildings
                        .SelectMany(b => b.Apartments)
                        .ToList();

                    var totalApartments = allApartments.Count;
                    var soldApartments = allApartments.Count(a => a.Status == "Đã Bán");
                    var unsoldApartments = totalApartments - soldApartments;

                    // Vẫn trả về project dù totalApartments = 0 (để hiển thị chart 0/0 hoặc 100% chưa bán)
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

        // GET: api/Dashboard/apartment-status-by-building
        [HttpGet("apartment-status-by-building")]
        [ProducesResponseType(typeof(ApiResponse<List<BuildingApartmentStatusDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<BuildingApartmentStatusDto>>>> GetApartmentStatusByBuilding()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleBuildingIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<List<BuildingApartmentStatusDto>>.Fail("Không thể xác định người dùng."));
                    }

                    accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();
                }

                var buildings = await _context.Buildings
                    .Where(b => b.IsActive && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(b.BuildingId))))
                    .Include(b => b.Apartments)
                    .ToListAsync();

                var result = buildings.Select(b =>
                {
                    var total = b.Apartments.Count;
                    var sold = b.Apartments.Count(a => a.Status == "Đã Bán");
                    var unsold = total - sold;
                    return new BuildingApartmentStatusDto
                    {
                        BuildingId = b.BuildingId,
                        BuildingName = b.Name ?? b.BuildingCode ?? "N/A",
                        TotalApartments = total,
                        SoldApartments = sold,
                        UnsoldApartments = unsold
                    };
                }).ToList();

                return Ok(ApiResponse<List<BuildingApartmentStatusDto>>.Success(result, "Lấy thống kê căn hộ theo tòa nhà thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<BuildingApartmentStatusDto>>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }

        // GET: api/Dashboard/revenue-by-project?year=2024
        [HttpGet("revenue-by-project")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectRevenueDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ProjectRevenueDto>>>> GetRevenueByProject([FromQuery] int? year = null)
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleBuildingIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<List<ProjectRevenueDto>>.Fail("Không thể xác định người dùng."));
                    }

                    accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();
                }

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
                        .Where(b => b.ProjectId == project.ProjectId && b.IsActive && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(b.BuildingId))))
                        .Select(b => b.BuildingId)
                        .ToListAsync();

                    // Manager: nếu project không có building nào thuộc quyền => không trả về project này
                    if (isManager && projectBuildings.Count == 0)
                    {
                        continue;
                    }

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

        // GET: api/Dashboard/revenue-by-building?year=2024
        [HttpGet("revenue-by-building")]
        [ProducesResponseType(typeof(ApiResponse<List<BuildingRevenueDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<BuildingRevenueDto>>>> GetRevenueByBuilding([FromQuery] int? year = null)
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleBuildingIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<List<BuildingRevenueDto>>.Fail("Không thể xác định người dùng."));
                    }

                    accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();
                }

                if (!year.HasValue)
                {
                    year = DateTime.UtcNow.Year;
                }

                var buildings = await _context.Buildings
                    .Where(b => b.IsActive && (!isManager || (accessibleBuildingIds != null && accessibleBuildingIds.Contains(b.BuildingId))))
                    .ToListAsync();

                var result = new List<BuildingRevenueDto>();

                foreach (var building in buildings)
                {
                    var buildingApartments = await _context.Apartments
                        .Where(a => a.BuildingId == building.BuildingId)
                        .Select(a => a.ApartmentId)
                        .ToListAsync();

                    var revenueByMonth = new List<MonthlyRevenueDto>();

                    for (int month = 1; month <= 12; month++)
                    {
                        var monthRevenue = await _context.Invoices
                            .Where(i => i.Status == "paid"
                                && i.CreatedAt.HasValue
                                && i.CreatedAt.Value.Year == year.Value
                                && i.CreatedAt.Value.Month == month
                                && buildingApartments.Contains(i.ApartmentId))
                            .SumAsync(i => (decimal?)i.Price) ?? 0;

                        revenueByMonth.Add(new MonthlyRevenueDto
                        {
                            Month = $"{month:D2}/{year}",
                            Revenue = monthRevenue
                        });
                    }

                    var totalRevenue = revenueByMonth.Sum(r => r.Revenue);

                    result.Add(new BuildingRevenueDto
                    {
                        BuildingId = building.BuildingId,
                        BuildingName = building.Name ?? building.BuildingCode ?? "N/A",
                        RevenueByMonth = revenueByMonth,
                        TotalRevenue = totalRevenue
                    });
                }

                return Ok(ApiResponse<List<BuildingRevenueDto>>.Success(result, "Lấy doanh thu theo tòa nhà thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<BuildingRevenueDto>>.Fail($"Lỗi khi lấy thống kê: {ex.Message}"));
            }
        }

        // GET: api/Dashboard/available-years
        [HttpGet("available-years")]
        [ProducesResponseType(typeof(ApiResponse<List<int>>), 200)]
        public async Task<ActionResult<ApiResponse<List<int>>>> GetAvailableYears()
        {
            try
            {
                var userId = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var isManager = !string.IsNullOrWhiteSpace(role) && role.Equals("manager", StringComparison.OrdinalIgnoreCase);

                List<string>? accessibleApartmentIds = null;
                if (isManager)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        return Unauthorized(ApiResponse<List<int>>.Fail("Không thể xác định người dùng."));
                    }

                    var accessibleBuildingIds = await _context.StaffBuildingAssignments
                        .Where(sba => sba.UserId == userId && sba.IsActive)
                        .Select(sba => sba.BuildingId)
                        .Distinct()
                        .ToListAsync();

                    accessibleApartmentIds = await _context.Apartments
                        .Where(a => accessibleBuildingIds.Contains(a.BuildingId))
                        .Select(a => a.ApartmentId)
                        .ToListAsync();
                }

                // Lấy danh sách các năm có invoice đã thanh toán
                var years = await _context.Invoices
                    .Where(i => i.Status == "paid" && i.CreatedAt.HasValue && (!isManager || (accessibleApartmentIds != null && accessibleApartmentIds.Contains(i.ApartmentId))))
                    .Select(i => i.CreatedAt.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();

                // Nếu không có năm nào, trả về năm hiện tại
                if (!years.Any())
                {
                    years.Add(DateTime.UtcNow.Year);
                }

                return Ok(ApiResponse<List<int>>.Success(years, "Lấy danh sách năm thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<int>>.Fail($"Lỗi khi lấy danh sách năm: {ex.Message}"));
            }
        }
    }
}

