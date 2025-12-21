using ApartaAPI.DTOs.Invoices;

namespace ApartaAPI.Services.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetUserInvoicesAsync(string userId);
    Task<List<InvoiceDto>> GetInvoicesAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null, string? feeType = null);
    Task<List<ApartmentInvoicesDto>> GetInvoicesGroupedByApartmentAsync(string buildingId, string userId, string? status = null, string? apartmentCode = null);
    Task<InvoiceDetailDto?> GetInvoiceDetailAsync(string invoiceId, string userId);
    Task<InvoiceDetailDto?> GetResidentInvoiceDetailAsync(string invoiceId, string userId);
    Task<(bool Success, string Message, int ProcessedCount)> GenerateInvoicesAsync(GenerateInvoicesRequest request, string userId);
    Task<(bool Success, string Message, InvoiceDto? Invoice)> CreateOneTimeInvoiceAsync(OneTimeInvoiceCreateDto dto, string userId, List<string>? imageUrls = null);
    Task<(bool Success, string Message)> MarkInvoiceAsPaidAsync(string invoiceId, string userId);
    Task<(bool Success, string Message)> DeleteInvoiceAsync(string invoiceId, string userId);
    Task<(bool Success, string Message)> UpdateInvoiceEndDateAsync(string invoiceId, DateOnly newEndDate, string userId);
    Task<(int SentCount, int FailedCount)> SendInvoiceEmailsToResidentsAsync(string buildingId, string billingPeriod);
    Task<(int SentCount, int FailedCount)> SendInvoiceSummaryToManagersAsync(string buildingId, string billingPeriod);
}

