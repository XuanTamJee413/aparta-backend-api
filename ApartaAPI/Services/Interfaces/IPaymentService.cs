namespace ApartaAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<string?> CreatePaymentLinkAsync(string invoiceId, string baseUrl);
    Task<bool> ProcessPaymentWebhookAsync(string? invoiceId, string orderCode);
}

