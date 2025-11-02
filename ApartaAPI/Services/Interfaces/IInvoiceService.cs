using ApartaAPI.DTOs;

namespace ApartaAPI.Services.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetUserInvoicesAsync(string userId);
    Task<List<InvoiceDto>> GetInvoicesAsync(string? status = null, string? apartmentCode = null);
    Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl);
    Task<bool> ProcessPaymentWebhookAsync(string? invoiceId, string orderCode);
}

