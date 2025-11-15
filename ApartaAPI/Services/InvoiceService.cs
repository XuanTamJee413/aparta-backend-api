using ApartaAPI.Data;
using ApartaAPI.DTOs.Invoices;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.PriceQuotations;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ApartaAPI.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApartaDbContext _context;
    private readonly IRepository<Invoice> _invoiceRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<MeterReading> _meterReadingRepo;
    private readonly IRepository<PriceQuotation> _priceQuotationRepo;
    private readonly IRepository<InvoiceItem> _invoiceItemRepo;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public InvoiceService(
        ApartaDbContext context,
        IRepository<Invoice> invoiceRepo,
        IRepository<User> userRepo,
        IRepository<MeterReading> meterReadingRepo,
        IRepository<PriceQuotation> priceQuotationRepo,
        IRepository<InvoiceItem> invoiceItemRepo,
        IMapper mapper,
        IConfiguration configuration)
    {
        _context = context;
        _invoiceRepo = invoiceRepo;
        _userRepo = userRepo;
        _meterReadingRepo = meterReadingRepo;
        _priceQuotationRepo = priceQuotationRepo;
        _invoiceItemRepo = invoiceItemRepo;
        _mapper = mapper;
        _configuration = configuration;
    }

    /**
    * Lấy danh sách hóa đơn của người dùng
    */
    public async Task<List<InvoiceDto>> GetUserInvoicesAsync(string userId)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || string.IsNullOrEmpty(user.ApartmentId)) return new List<InvoiceDto>();

        var invoices = await _context.Invoices
            .Include(i => i.Apartment)
            .ThenInclude(a => a.Users)
            .ThenInclude(u => u.Role)
            .Include(i => i.Staff)
            .Where(i => i.ApartmentId == user.ApartmentId)
            .ToListAsync();

        // Map to DTOs with resident name
        var result = invoices.Select(invoice =>
        {
            var dto = _mapper.Map<InvoiceDto>(invoice);
            // Get resident name: prefer user with role "resident", otherwise first user
            var resident = invoice.Apartment.Users
                .FirstOrDefault(u => u.Role.RoleName.ToLower() == "resident") 
                ?? invoice.Apartment.Users.FirstOrDefault();
            dto.ResidentName = resident?.Name;
            return dto;
        }).ToList();
        return result;
    }

    /**
    * Lấy danh sách hóa đơn của tòa nhà
    */
    public async Task<List<InvoiceDto>> GetInvoicesAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null)
    {
        // 1) Authorization: ensure user has access to the project's building
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        if (building == null)
        {
            return new List<InvoiceDto>();
        }

        // TODO: Authorization check - Tạm thời bỏ qua logic kiểm tra quyền theo project
        // Vì staff có thể quản lý tất cả mọi project, không chỉ một project
        // Logic này sẽ được sửa lại sau
        // var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);
        // if (user == null)
        // {
        //     return new List<InvoiceDto>();
        // }
        //
        // // Load user projects for access check
        // var userWithProjects = await _context.Users
        //     .Include(u => u.Projects)
        //     .FirstOrDefaultAsync(u => u.UserId == userId);
        //
        // var hasProjectAccess = userWithProjects?.Projects?.Any(p => p.ProjectId == building.ProjectId) == true;
        // if (!hasProjectAccess)
        // {
        //     // No access -> return empty
        //     return new List<InvoiceDto>();
        // }

        // 2) Query invoices filtered by building
        // Sử dụng join rõ ràng để tránh lỗi navigation property trong Where clause
        var joinedQuery = _context.Invoices
            .Join(
                _context.Apartments,
                invoice => invoice.ApartmentId,
                apartment => apartment.ApartmentId,
                (invoice, apartment) => new { Invoice = invoice, Apartment = apartment }
            )
            .Where(x => x.Apartment.BuildingId == buildingId);

        // Filter by status (optional)
        if (!string.IsNullOrWhiteSpace(status))
        {
            joinedQuery = joinedQuery.Where(x => x.Invoice.Status == status);
        }

        // Filter by apartment code (optional)
        if (!string.IsNullOrWhiteSpace(apartmentCode))
        {
            var code = apartmentCode.Trim().ToLower();
            joinedQuery = joinedQuery.Where(x => x.Apartment.Code.ToLower().Contains(code));
        }

        // Lấy danh sách Invoice IDs sau khi filter
        var invoiceIds = await joinedQuery
            .Select(x => x.Invoice.InvoiceId)
            .ToListAsync();

        var invoices = await _context.Invoices
            .Where(i => invoiceIds.Contains(i.InvoiceId))
            .Include(i => i.Apartment)
            .ThenInclude(a => a.Users)
            .ThenInclude(u => u.Role)
            .Include(i => i.Staff)
            .OrderByDescending(i => i.StartDate) // Newest period first
            .ToListAsync();

        var result = invoices.Select(invoice =>
        {
            var dto = _mapper.Map<InvoiceDto>(invoice);
            var resident = invoice.Apartment.Users
                .FirstOrDefault(u => u.Role.RoleName.ToLower() == "resident") 
                ?? invoice.Apartment.Users.FirstOrDefault();
            dto.ResidentName = resident?.Name;
            return dto;
        }).ToList();

        return result;
    }

    /**
     * Lấy danh sách hóa đơn của tòa nhà (grouped by apartment)
     */
    public async Task<List<ApartmentInvoicesDto>> GetInvoicesGroupedByApartmentAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null)
    {
        // Lấy danh sách invoices (dùng lại logic từ GetInvoicesAsync)
        var invoices = await GetInvoicesAsync(buildingId, userId, status, apartmentCode);

        // Group by apartment
        var groupedResult = invoices
            .GroupBy(i => new { i.ApartmentId, i.ApartmentCode, i.ResidentName })
            .Select(g => new ApartmentInvoicesDto
            {
                ApartmentId = g.Key.ApartmentId,
                ApartmentCode = g.Key.ApartmentCode,
                ResidentName = g.Key.ResidentName,
                Invoices = g.OrderByDescending(i => i.StartDate).ToList(),
                TotalAmount = g.Sum(i => i.Price)
            })
            .OrderBy(x => x.ApartmentCode)
            .ToList();

        return groupedResult;
    }

    /**
     * Lấy thông tin chi tiết của một hóa đơn
     */
    public async Task<InvoiceDetailDto?> GetInvoiceDetailAsync(string invoiceId, string userId)
    {
        // Truy vấn Hóa đơn tổng với Include InvoiceItems
        var invoice = await _context.Invoices
            .Include(i => i.Apartment)
            .ThenInclude(a => a.Users)
            .ThenInclude(u => u.Role)
            .Include(i => i.Staff)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
        {
            return null;
        }

        // Sử dụng AutoMapper để map Invoice -> InvoiceDetailDto
        var detailDto = _mapper.Map<InvoiceDetailDto>(invoice);
        return detailDto;
    }

    /**
    * Tạo hóa đơn cho tòa nhà
    */
    public async Task<(bool Success, string Message, int ProcessedCount)> GenerateInvoicesAsync(GenerateInvoicesRequest request, string userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Xác định kỳ tính toán (mặc định là tháng hiện tại nếu không truyền)
            var billingPeriod = string.IsNullOrWhiteSpace(request.BillingPeriod)
                ? DateTime.Now.ToString("yyyy-MM")
                : request.BillingPeriod;

            // Nạp thông tin tòa nhà để kiểm tra trạng thái và dùng cấu hình chung
            var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == request.BuildingId);
            if (building == null)
            {
                await transaction.RollbackAsync();
                return (false, ApiResponse.SM01_NO_RESULTS, 0);
            }

            if (!building.IsActive)
            {
                await transaction.RollbackAsync();
                return (false, ApiResponse.SM26_BUILDING_NOT_ACTIVE, 0);
            }

            // Xác định staffId: nếu userId là "SYSTEM_JOB" thì set null, ngược lại kiểm tra user có tồn tại không
            string? staffId = null;
            if (!string.IsNullOrEmpty(userId) && userId != "SYSTEM_JOB")
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (userExists)
                {
                    staffId = userId;
                }
                // Nếu user không tồn tại, staffId sẽ là null (cho phép tạo hóa đơn mà không có staff)
            }

            // issueDate = ngày chạy job hiện tại, dueDate = +5 ngày theo yêu cầu
            var issueDate = DateOnly.FromDateTime(DateTime.Today);
            var dueDate = issueDate.AddDays(5);

            // Lấy danh sách căn hộ đang thuê để tạo hóa đơn
            var apartments = await _context.Apartments
                .Include(a => a.ApartmentMembers)
                .Where(a => a.BuildingId == request.BuildingId && a.Status == "Đã thuê")
                .ToListAsync();

            if (!apartments.Any())
            {
                await transaction.RollbackAsync();
                return (true, ApiResponse.SM20_NO_CHANGES, 0);
            }

            var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();

            // Lấy toàn bộ bảng giá của tòa nhà (bao gồm mọi phương thức tính)
            var priceQuotations = await _context.PriceQuotations
                .Where(pq => pq.BuildingId == request.BuildingId)
                .ToListAsync();

            if (!priceQuotations.Any())
            {
                await transaction.RollbackAsync();
                return (true, ApiResponse.SM20_NO_CHANGES, 0);
            }

            // Chuẩn bị các hóa đơn PENDING đã có sẵn cùng kỳ để tái sử dụng
            var existingInvoices = await _context.Invoices
                .Where(i =>
                    apartmentIds.Contains(i.ApartmentId) &&
                    i.Status == "PENDING" &&
                    i.Description != null &&
                    i.Description.Contains($"BillingPeriod: {billingPeriod}"))
                .Include(i => i.InvoiceItems)
                .ToListAsync();

            // Gom nhóm theo căn hộ để tra cứu nhanh hóa đơn hiện tại
            var invoiceLookup = existingInvoices
                .GroupBy(i => i.ApartmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(inv => inv.CreatedAt ?? DateTime.MinValue).First());

            // Lấy các chỉ số đồng hồ chưa được khóa trong kỳ hiện tại
            var currentReadings = await _context.MeterReadings
                .Where(mr =>
                    apartmentIds.Contains(mr.ApartmentId) &&
                    mr.BillingPeriod == billingPeriod &&
                    mr.InvoiceItemId == null)
                .ToListAsync();

            // Lập bảng tra giúp lấy chỉ số mới nhất theo (Căn hộ, Loại phí)
            var readingLookup = currentReadings
                .GroupBy(mr => (mr.ApartmentId, mr.FeeType))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt ?? DateTime.MinValue).First());

            int processedCount = 0;

            foreach (var apartment in apartments)
            {
                if (!invoiceLookup.TryGetValue(apartment.ApartmentId, out var invoice))
                {
                    // Chưa có hóa đơn cùng kỳ -> tạo hóa đơn mới
                    invoice = new Invoice
                    {
                        InvoiceId = Guid.NewGuid().ToString("N"),
                        ApartmentId = apartment.ApartmentId,
                        StaffId = staffId,
                        FeeType = "MONTHLY_BILLING",
                        Price = 0,
                        Status = "PENDING",
                        Description = $"BillingPeriod: {billingPeriod}",
                        StartDate = issueDate,
                        EndDate = dueDate,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Invoices.Add(invoice);
                    invoiceLookup[apartment.ApartmentId] = invoice;
                }
                else
                {
                    // Đã có hóa đơn thì cập nhật thông tin chung (ngày, người xử lý)
                    invoice.FeeType = "MONTHLY_BILLING";
                    invoice.StaffId = staffId;
                    invoice.StartDate = issueDate;
                    invoice.EndDate = dueDate;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                // Khởi đầu tổng tiền bằng các invoice item đã tồn tại (nếu có)
                decimal runningTotal = invoice.InvoiceItems.Sum(ii => ii.Total);

                foreach (var rule in priceQuotations)
                {
                    var description = $"{rule.FeeType} - {billingPeriod}";

                    // Tránh tạo trùng invoice item cho cùng loại phí trong cùng kỳ
                    if (invoice.InvoiceItems.Any(ii => ii.FeeType == rule.FeeType && ii.Description == description))
                    {
                        continue;
                    }

                    decimal quantityValue = 0;
                    decimal lineTotal = 0;
                    MeterReading? matchedReading = null;

                    switch (rule.CalculationMethod)
                    {
                        case "FIXED":
                            quantityValue = 1;
                            lineTotal = rule.UnitPrice;
                            break;

                        case "PER_AREA":
                            quantityValue = (decimal)(apartment.Area ?? 0);
                            lineTotal = quantityValue * rule.UnitPrice;
                            break;

                        case "PER_PERSON":
                            quantityValue = apartment.ApartmentMembers.Count;
                            lineTotal = quantityValue * rule.UnitPrice;
                            break;

                        case "PER_ITEM":
                            quantityValue = 1;
                            lineTotal = quantityValue * rule.UnitPrice;
                            break;

                        case "PER_UNIT_METER":
                        case "TIERED":
                            // Tìm chỉ số hiện tại (chưa khóa) cho loại phí này
                            if (!readingLookup.TryGetValue((apartment.ApartmentId, rule.FeeType), out matchedReading))
                            {
                                // Không có chỉ số thì bỏ qua (không tạo invoice item)
                                // Lý do: Không thể tính tiền nếu không có chỉ số đồng hồ
                                continue;
                            }

                            // Tìm chỉ số kỳ trước để tính lượng tiêu thụ
                            var previousReading = await _context.MeterReadings
                                .Where(mr =>
                                    mr.ApartmentId == apartment.ApartmentId &&
                                    mr.FeeType == rule.FeeType &&
                                    mr.BillingPeriod != null &&
                                    mr.BillingPeriod.CompareTo(billingPeriod) < 0)
                                .OrderByDescending(mr => mr.BillingPeriod)
                                .FirstOrDefaultAsync();

                            var previousValue = previousReading?.ReadingValue ?? 0;
                            quantityValue = matchedReading.ReadingValue - previousValue;
                            
                            // Đảm bảo lượng tiêu thụ không âm
                            if (quantityValue < 0)
                            {
                                quantityValue = 0;
                            }

                            // Tính tiền theo phương thức
                            if (string.Equals(rule.CalculationMethod, "TIERED", StringComparison.OrdinalIgnoreCase))
                            {
                                lineTotal = CalculateTieredPrice(quantityValue, rule.Note);
                                if (lineTotal <= 0)
                                {
                                    // Nếu dữ liệu bậc lỗi, fallback đơn giá mặc định
                                    lineTotal = quantityValue * rule.UnitPrice;
                                }
                            }
                            else
                            {
                                lineTotal = quantityValue * rule.UnitPrice;
                            }
                            break;

                        default:
                            quantityValue = 1;
                            lineTotal = rule.UnitPrice;
                            break;
                    }

                    // Chỉ bỏ qua nếu:
                    // 1. Không phát sinh tiền (lineTotal <= 0)
                    // 2. KHÔNG có chỉ số đồng hồ cần khóa (matchedReading == null)
                    // 3. KHÔNG phải là loại phí bắt buộc (FIXED, PER_AREA, etc.)
                    if (lineTotal <= 0 && matchedReading == null)
                    {
                        // Không phát sinh tiền và không có chỉ số để khóa -> bỏ qua
                        continue;
                    }

                    // Ghi nhận chi tiết hóa đơn - sử dụng AutoMapper với custom mapping
                    var invoiceItem = _mapper.Map<InvoiceItem>(rule);
                    invoiceItem.InvoiceItemId = Guid.NewGuid().ToString("N");
                    invoiceItem.InvoiceId = invoice.InvoiceId;
                    invoiceItem.Description = description;
                    invoiceItem.Quantity = Convert.ToInt32(Math.Round(quantityValue, MidpointRounding.AwayFromZero)); //làm tròn 
                    invoiceItem.UnitPrice = DetermineUnitPrice(rule, quantityValue, lineTotal);
                    invoiceItem.Total = lineTotal;
                    invoiceItem.CreatedAt = DateTime.UtcNow;
                    invoiceItem.UpdatedAt = null;

                    _context.InvoiceItems.Add(invoiceItem);
                    invoice.InvoiceItems.Add(invoiceItem);

                    if (matchedReading != null)
                    {
                        // Khóa chỉ số sau khi đã gắn vào invoice item
                        matchedReading.InvoiceItemId = invoiceItem.InvoiceItemId;
                        _context.MeterReadings.Update(matchedReading);
                    }

                    runningTotal += lineTotal;
                    processedCount++;
                }

                invoice.Price = runningTotal;
                invoice.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = ApiResponse.SM38_INVOICE_GENERATE_SUCCESS.Replace("{count}", processedCount.ToString());
            return (true, message, processedCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
            }
            return (false, ApiResponse.SM40_SYSTEM_ERROR, 0);
        }
    }

    private static decimal DetermineUnitPrice(PriceQuotation rule, decimal quantityValue, decimal lineTotal)
    {
        if (string.Equals(rule.CalculationMethod, "TIERED", StringComparison.OrdinalIgnoreCase))
        {
            if (quantityValue <= 0)
            {
                return 0;
            }

            return Math.Round(lineTotal / quantityValue, 2, MidpointRounding.AwayFromZero);
        }

        return rule.UnitPrice;
    }

    private decimal CalculateTieredPrice(decimal quantity, string? tiersJson)
    {
        if (quantity <= 0 || string.IsNullOrWhiteSpace(tiersJson))
        {
            return 0;
        }

        try
        {
            var tiers = JsonSerializer.Deserialize<List<TierDefinitionDto>>(tiersJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tiers == null || tiers.Count == 0)
            {
                return 0;
            }

            decimal total = 0;
            decimal consumed = 0;
            var orderedTiers = tiers.OrderBy(t => t.FromValue).ToList();

            foreach (var tier in orderedTiers)
            {
                var lowerBound = Math.Max(tier.FromValue, consumed);
                if (quantity <= lowerBound)
                {
                    continue;
                }

                var upperBound = tier.ToValue ?? decimal.MaxValue;
                var applicableUpper = Math.Min(quantity, upperBound);
                if (applicableUpper <= lowerBound)
                {
                    continue;
                }

                var tierQuantity = applicableUpper - lowerBound;
                total += tierQuantity * tier.UnitPrice;
                consumed = applicableUpper;

                if (consumed >= quantity)
                {
                    break;
                }
            }

            if (consumed < quantity && orderedTiers.Count > 0)
            {
                var lastTier = orderedTiers.Last();
                total += (quantity - consumed) * lastTier.UnitPrice;
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

}

