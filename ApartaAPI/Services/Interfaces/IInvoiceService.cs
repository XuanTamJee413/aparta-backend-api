using ApartaAPI.DTOs;

namespace ApartaAPI.Services.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetUserInvoicesAsync(string userId);
    Task<List<InvoiceDto>> GetInvoicesAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null);
    Task<List<ApartmentInvoicesDto>> GetInvoicesGroupedByApartmentAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null);
    Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl);
    Task<bool> ProcessPaymentWebhookAsync(string? invoiceId, string orderCode);
    Task<(bool Success, string Message, int ProcessedCount)> GenerateInvoicesAsync(GenerateInvoicesRequest request, string userId);
}

