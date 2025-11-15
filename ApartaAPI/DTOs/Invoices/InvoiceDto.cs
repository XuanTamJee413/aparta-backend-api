namespace ApartaAPI.DTOs.Invoices;

public class InvoiceDto
{
    public string InvoiceId { get; set; } = null!;
    public string ApartmentId { get; set; } = null!;
    public string ApartmentCode { get; set; } = null!;
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string FeeType { get; set; } = null!;
    public decimal Price { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ResidentName { get; set; }
}

public class InvoiceQueryParameters
{
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public string? Month { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } 
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GenerateInvoicesRequest
{
    public string BuildingId { get; set; } = null!;
    public string? BillingPeriod { get; set; } 
}

public class ApartmentInvoicesDto
{
    public string ApartmentId { get; set; } = null!;
    public string ApartmentCode { get; set; } = null!;
    public string? ResidentName { get; set; }
    public List<InvoiceDto> Invoices { get; set; } = new List<InvoiceDto>();
    public decimal TotalAmount { get; set; }
}

public class InvoiceItemDto
{
    public string InvoiceItemId { get; set; } = null!;
    public string InvoiceId { get; set; } = null!;
    public string FeeType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InvoiceDetailDto
{
    public string InvoiceId { get; set; } = null!;
    public string ApartmentId { get; set; } = null!;
    public string ApartmentCode { get; set; } = null!;
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string FeeType { get; set; } = null!;
    public decimal Price { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ResidentName { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
}

