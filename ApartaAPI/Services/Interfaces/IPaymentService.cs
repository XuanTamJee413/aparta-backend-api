using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl);
    Task<bool> ProcessPaymentWebhookAsync(string? invoiceId, string orderCode);
    Task<Payment?> GetPaymentByOrderCodeAsync(string orderCode);
    Task<Project?> GetProjectFromPaymentAsync(string paymentId);
}

