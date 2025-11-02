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
    private readonly IMapper _mapper;
    private readonly PayOSService _payOSService;
    private readonly IConfiguration _configuration;

    public InvoiceService(
        ApartaDbContext context,
        IRepository<Invoice> invoiceRepo,
        IRepository<Payment> paymentRepo,
        IRepository<User> userRepo,
        IMapper mapper,
        PayOSService payOSService,
        IConfiguration configuration)
    {
        _context = context;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _userRepo = userRepo;
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
}

