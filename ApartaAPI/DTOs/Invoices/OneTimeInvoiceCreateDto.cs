using Microsoft.AspNetCore.Http;

namespace ApartaAPI.DTOs.Invoices;

public class OneTimeInvoiceCreateDto
{
    public string ApartmentId { get; set; } = null!;
    
    public string? PriceQuotationId { get; set; }
    
    public string ItemDescription { get; set; } = null!;
    
    public decimal Amount { get; set; }
    
    public string? Note { get; set; }
    
    public List<IFormFile>? Images { get; set; }
}

