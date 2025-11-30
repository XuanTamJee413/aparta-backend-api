using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApartaAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly ApartaDbContext _context;
    private readonly IRepository<Invoice> _invoiceRepo;
    private readonly IRepository<Payment> _paymentRepo;
    private readonly PayOSService _payOSService;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ApartaDbContext context,
        IRepository<Invoice> invoiceRepo,
        IRepository<Payment> paymentRepo,
        PayOSService payOSService,
        IConfiguration configuration)
    {
        _context = context;
        _invoiceRepo = invoiceRepo;
        _paymentRepo = paymentRepo;
        _payOSService = payOSService;
        _configuration = configuration;
    }

    /**
    * Tạo link thanh toán cho hóa đơn
    */
    public async Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl)
    {
        // Get invoice với thông tin project
        var invoice = await _context.Invoices
            .Include(i => i.Apartment)
                .ThenInclude(a => a.Building)
                    .ThenInclude(b => b.Project)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

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

        // Lấy project để lấy PayOS settings
        var project = invoice.Apartment?.Building?.Project;
        if (project == null)
        {
            Console.WriteLine($"Project not found for invoice: {invoiceId}");
            throw new Exception("Không tìm thấy thông tin dự án cho hóa đơn này");
        }

        // Tạo PayOSService với settings từ project (fallback về config nếu project chưa có)
        var payOSService = PayOSService.CreateFromProject(
            project.PayOSClientId,
            project.PayOSApiKey,
            project.PayOSChecksumKey,
            _configuration
        );

        var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
        var cancelUrl = $"{baseUrl}/api/payos/payment/cancel";
        var returnUrl = $"{baseUrl}/api/payos/payment/success?invoiceId={invoiceId}";
        
        Console.WriteLine($"Creating PayOS payment for invoice {invoiceId}, Amount: {invoice.Price}, Project: {project.ProjectId}");
        
        var paymentResult = await payOSService.CreatePaymentAsync(
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
    * Lấy payment record từ orderCode
    */
    public async Task<Payment?> GetPaymentByOrderCodeAsync(string orderCode)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentCode == orderCode);
    }

    /**
    * Lấy project từ paymentId (thông qua invoice -> apartment -> building -> project)
    */
    public async Task<Project?> GetProjectFromPaymentAsync(string paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Apartment)
                    .ThenInclude(a => a.Building)
                        .ThenInclude(b => b.Project)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        return payment?.Invoice?.Apartment?.Building?.Project;
    }
}

