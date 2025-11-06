using ApartaAPI.Data;
using ApartaAPI.DTOs;
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

    public async Task<List<InvoiceDto>> GetUserInvoicesAsync(string userId)
    {
        // Get user and their apartment
        var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null || string.IsNullOrEmpty(user.ApartmentId))
        {
            return new List<InvoiceDto>();
        }

        // Get all invoices for user's apartment with navigation properties
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

    public async Task<List<InvoiceDto>> GetInvoicesAsync(string? status = null, string? apartmentCode = null)
    {
        // Get all invoices with navigation properties
        var query = _context.Invoices
            .Include(i => i.Apartment)
            .ThenInclude(a => a.Users)
            .ThenInclude(u => u.Role)
            .Include(i => i.Staff)
            .AsQueryable();

        // Filter by status (optional)
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(i => i.Status == status);
        }

        // Filter by apartment code (optional)
        if (!string.IsNullOrWhiteSpace(apartmentCode))
        {
            var code = apartmentCode.Trim().ToLower();
            query = query.Where(i => i.Apartment.Code.ToLower().Contains(code));
        }

        // Get all invoices (no pagination)
        var invoices = await query
            .OrderByDescending(i => i.CreatedAt) // Newest first
            .ToListAsync();

        // Map to DTOs with resident name
        var result = invoices.Select(invoice =>
        {
            var dto = _mapper.Map<InvoiceDto>(invoice);
            // Lấy cư dân có role là "resident", nếu không có thì lấy user đầu tiên
            var resident = invoice.Apartment.Users
                .FirstOrDefault(u => u.Role.RoleName.ToLower() == "resident") 
                ?? invoice.Apartment.Users.FirstOrDefault();
            dto.ResidentName = resident?.Name;
            return dto;
        }).ToList();

        return result;
    }

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
                return (true, "Không có chỉ số mới nào cần xử lý.", 0);
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
                    // issueDate là ngày hôm tạo (ngày hiện tại)
                    var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    // dueDate là 15 tháng sau issueDate
                    var dueDate = issueDate.AddMonths(15);
                    
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

            return (true, $"Đã xử lý thành công {processedCount} chỉ số.", processedCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi khi xử lý: {ex.Message}", 0);
        }
    }
}

