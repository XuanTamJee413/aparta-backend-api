using ApartaAPI.Data;
using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApartaAPI.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApartaDbContext _context;
    private readonly IRepository<Invoice> _invoiceRepo;
    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<MeterReading> _meterReadingRepo;
    private readonly IRepository<PriceQuotation> _priceQuotationRepo;
    private readonly IRepository<InvoiceItem> _invoiceItemRepo;
    private readonly IMapper _mapper;
    private readonly PayOSService _payOSService;
    private readonly IConfiguration _configuration;

    public InvoiceService(
        ApartaDbContext context,
        IRepository<Invoice> invoiceRepo,
        IRepository<Payment> paymentRepo,
        IRepository<User> userRepo,
        IRepository<MeterReading> meterReadingRepo,
        IRepository<PriceQuotation> priceQuotationRepo,
        IRepository<InvoiceItem> invoiceItemRepo,
        IMapper mapper,
        PayOSService payOSService,
        IConfiguration configuration)
    {
        _context = context;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _userRepo = userRepo;
        _meterReadingRepo = meterReadingRepo;
        _priceQuotationRepo = priceQuotationRepo;
        _invoiceItemRepo = invoiceItemRepo;
        _mapper = mapper;
        _payOSService = payOSService;
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
    * Tạo link thanh toán cho hóa đơn
    */
    public async Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl)
    {
        // Get invoice
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            Console.WriteLine($"Invoice not found: {invoiceId}");
            return null;
        }

        if (invoice.Status != "PENDING")
        {
            Console.WriteLine($"Invoice status is not PENDING: {invoiceId}, Status: {invoice.Status}");
            throw new Exception($"Chỉ có thể thanh toán hóa đơn có trạng thái PENDING. Hóa đơn này có trạng thái: {invoice.Status}");
        }

        // Check minimum amount (PayOS might require minimum amount)
        if (invoice.Price <= 0)
        {
            Console.WriteLine($"Invoice amount is invalid: {invoiceId}, Price: {invoice.Price}");
            return null;
        }

        var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
        var cancelUrl = $"{baseUrl}/api/payos/payment/cancel";
        var returnUrl = $"{baseUrl}/api/payos/payment/success?invoiceId={invoiceId}";
        
        Console.WriteLine($"Creating PayOS payment for invoice {invoiceId}, Amount: {invoice.Price}");
        
        var paymentResult = await _payOSService.CreatePaymentAsync(
            invoiceId,
            invoice.Price,
            invoice.Description ?? $"Thanh toán hóa đơn {invoiceId}",
            cancelUrl,
            returnUrl
        );

        if (!paymentResult.IsSuccess || paymentResult.Data == null)
        {
            var errorMsg = paymentResult.ErrorMessage ?? "PayOS payment creation failed";
            Console.WriteLine($"PayOS payment creation failed for invoice {invoiceId}: {errorMsg}");
            throw new Exception(errorMsg);
        }

        // Create payment record with orderCode for webhook lookup
        // PaymentCode stores orderCode (long) as string for lookup in webhook
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid().ToString(),
            InvoiceId = invoiceId,
            Amount = invoice.Price,
            PaymentMethod = "PAYOS",
            PaymentDate = DateOnly.FromDateTime(DateTime.Now),
            Status = "PENDING",
            PaymentCode = paymentResult.Data.orderCode.ToString(), // Store orderCode for webhook lookup
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _paymentRepo.AddAsync(payment);
        
        Console.WriteLine($"Payment record created: InvoiceId={invoiceId}, OrderCode={paymentResult.Data.orderCode}, PaymentCode={payment.PaymentCode}");

        Console.WriteLine($"Payment link created successfully for invoice {invoiceId}: {paymentResult.Data.checkoutUrl}");
        return paymentResult.Data.checkoutUrl;
    }

    /**
    * Xử lý webhook thanh toán
    */
    public async Task<bool> ProcessPaymentWebhookAsync(string? invoiceId, string orderCode)
    {
        try
        {
            // Find payment record by orderCode (stored in PaymentCode)
            // This is the most reliable way since description format changed
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentCode == orderCode);
            
            if (payment == null)
            {
                Console.WriteLine($"Payment record not found for orderCode={orderCode}");
                return false;
            }

            // Get invoice from payment record
            var invoice = await _invoiceRepo.GetByIdAsync(payment.InvoiceId);
            if (invoice == null)
            {
                Console.WriteLine($"Invoice not found for InvoiceId={payment.InvoiceId}");
                return false;
            }

            // Update invoice status
            invoice.Status = "PAID";
            invoice.UpdatedAt = DateTime.Now;
            await _invoiceRepo.UpdateAsync(invoice);

            // Update payment record status
            payment.Status = "COMPLETED";
            payment.UpdatedAt = DateTime.Now;
            await _paymentRepo.UpdateAsync(payment);

            Console.WriteLine($"Successfully updated invoice {invoice.InvoiceId} to PAID status for orderCode={orderCode}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing payment webhook: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return false;
        }
    }

    /**
    * Tạo hóa đơn cho tòa nhà
    */
    public async Task<(bool Success, string Message, int ProcessedCount)> GenerateInvoicesAsync(GenerateInvoicesRequest request, string userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Xác định billingPeriod (nếu null thì lấy tháng hiện tại)
            var billingPeriod = request.BillingPeriod;
            if (string.IsNullOrWhiteSpace(billingPeriod))
            {
                billingPeriod = DateTime.Now.ToString("yyyy-MM");
            }

            // Tải Chỉ số Cần tính (Meter_Reading)
            var readingsToProcess = await _context.MeterReadings
                .Include(mr => mr.Apartment)
                .Where(mr => 
                    mr.Apartment.BuildingId == request.BuildingId &&
                    mr.BillingPeriod == billingPeriod &&
                    mr.InvoiceItemId == null)//Chỉ lấy những meter reading chưa được tạo thanh toán
                .ToListAsync();

            // Lọc các MeterReading có fee_type thuộc calculation_method == "PER_UNIT_METER"
            var priceQuotations = await _priceQuotationRepo.FindAsync(pq =>
                pq.BuildingId == request.BuildingId &&
                pq.CalculationMethod == "PER_UNIT_METER");

            //lọc giá theo price quotation
            var validFeeTypes = priceQuotations.Select(pq => pq.FeeType).Distinct().ToList();
            readingsToProcess = readingsToProcess
                .Where(mr => validFeeTypes.Contains(mr.FeeType))
                .ToList();

            if (!readingsToProcess.Any())
            {
                await transaction.RollbackAsync();
                return (true, ApiResponse.SM01_NO_RESULTS, 0);
            }

            // Tải Bảng giá (Price_Quotation)
            var priceLookup = priceQuotations
                .GroupBy(pq => pq.FeeType)
                .ToDictionary(g => g.Key, g => g.First().UnitPrice);

            // Phân nhóm theo apartment_id để xử lý từng căn hộ
            var apartmentGroups = readingsToProcess
                .GroupBy(mr => mr.ApartmentId)
                .ToList();

            int processedCount = 0;

            //Vòng lặp (Theo Căn hộ)
            foreach (var apartmentGroup in apartmentGroups)
            {
                var apartmentId = apartmentGroup.Key;
                var readingsForThisApartment = apartmentGroup.ToList();

                // Tìm Hóa đơn "Pending" cho căn hộ và billingPeriod
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => 
                        i.ApartmentId == apartmentId &&
                        i.Status == "PENDING" && //Chỉ lấy hóa đơn chưa thanh toán
                        i.Description != null &&
                        i.Description.Contains($"BillingPeriod: {billingPeriod}"));

                Invoice invoiceToUse;
                decimal runningTotal;

                if (existingInvoice == null)
                {
                    // 2. Parse chuỗi "yyyy-MM" thành ngày 1 của tháng đó
                    DateTime firstDayOfBillingMonth;
                    try
                    {
                        firstDayOfBillingMonth = DateTime.ParseExact(
                            $"{billingPeriod}-01",
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture
                        );
                    }
                    catch (FormatException)
                    {
                        return (false, ApiResponse.SM25_INVALID_INPUT, 0);
                    }

                    // 3. Tính ngày 1 của tháng TIẾP THEO (Yêu cầu của bạn)
                    DateTime firstDayOfNextMonth = firstDayOfBillingMonth.AddMonths(1);

                    // 4. Chuyển đổi sang DateOnly (Vì Bảng Invoice của bạn dùng DateOnly)
                    var issueDate = DateOnly.FromDateTime(firstDayOfNextMonth);
                    var dueDate = issueDate.AddDays(14); 

                    invoiceToUse = new Invoice
                    {
                        InvoiceId = Guid.NewGuid().ToString("N"),
                        ApartmentId = apartmentId,
                        StaffId = userId, // Lưu userId của người đăng nhập thực hiện API
                        FeeType = "METER_BILLING", // Loại phí tổng hợp
                        Price = 0, // Sẽ được cập nhật sau
                        Status = "PENDING",
                        Description = $"BillingPeriod: {billingPeriod}",
                        StartDate = issueDate,
                        EndDate = dueDate,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = null
                    };
                    await _invoiceRepo.AddAsync(invoiceToUse);
                    runningTotal = 0;
                }
                else
                {
                    // Dùng Lại Invoice
                    invoiceToUse = existingInvoice;
                    runningTotal = existingInvoice.Price;
                }

                // Vòng lặp  (Theo Chỉ số)
                foreach (var currentReading in readingsForThisApartment)
                {
                    // Tính Tiêu thụ (quantity)
                    // Tìm Chỉ số CŨ
                    var previousReading = await _meterReadingRepo.FindAsync(mr =>
                        mr.ApartmentId == currentReading.ApartmentId &&
                        mr.FeeType == currentReading.FeeType &&
                        mr.BillingPeriod != null &&
                        mr.BillingPeriod.CompareTo(billingPeriod) < 0);

                    var latestPreviousReading = previousReading
                        .Where(mr => !string.IsNullOrEmpty(mr.BillingPeriod))
                        .OrderByDescending(mr => mr.BillingPeriod)
                        .FirstOrDefault();

                    decimal oldValue = latestPreviousReading?.ReadingValue ?? 0;
                    decimal quantity = currentReading.ReadingValue - oldValue;

                    // Tính Tiền
                    if (!priceLookup.TryGetValue(currentReading.FeeType, out var unitPrice))
                    {
                        // Nếu không tìm thấy giá, bỏ qua chỉ số này và tiếp tục với chỉ số tiếp theo
                        continue;
                    }

                    decimal lineTotal = quantity * unitPrice;

                    // Tạo Invoice_Item để lưu trữ chi tiết thanh toán
                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceItemId = Guid.NewGuid().ToString("N"),
                        InvoiceId = invoiceToUse.InvoiceId,
                        FeeType = currentReading.FeeType,
                        Description = $"{currentReading.FeeType} - {billingPeriod}",
                        Quantity = (int)Math.Round(quantity, 0), // Convert to int
                        UnitPrice = unitPrice,
                        Total = lineTotal,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = null
                    };

                    await _invoiceItemRepo.AddAsync(invoiceItem);

                    // Khóa Chỉ số để tránh trùng lặp
                    currentReading.InvoiceItemId = invoiceItem.InvoiceItemId;
                    await _meterReadingRepo.UpdateAsync(currentReading);

                    // Cộng dồn Tổng tiền để tính toán tổng tiền thanh toán
                    runningTotal += lineTotal;
                    processedCount++;
                }

                // Cập nhật Hóa đơn Tổng
                invoiceToUse.Price = runningTotal;
                invoiceToUse.UpdatedAt = DateTime.UtcNow;
                await _invoiceRepo.UpdateAsync(invoiceToUse);
            }

            // Lưu tất cả thay đổi vào cơ sở dữ liệu để lưu trữ trong DB
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var message = ApiResponse.SM38_INVOICE_GENERATE_SUCCESS.Replace("{count}", processedCount.ToString());
            return (true, message, processedCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ApiResponse.SM40_SYSTEM_ERROR, 0);
        }
    }
}

