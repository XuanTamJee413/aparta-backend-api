namespace ApartaAPI.DTOs.PayOS;

public class PaymentDataDto
{
    public string bin { get; set; } = null!;
    public string accountNumber { get; set; } = null!;
    public string accountName { get; set; } = null!;
    public long amount { get; set; }
    public string description { get; set; } = null!;
    public long orderCode { get; set; }
    public string currency { get; set; } = null!;
    public string paymentLinkId { get; set; } = null!;
    public string status { get; set; } = null!;
    public DateTime createdAt { get; set; }
    public DateTime? transactions { get; set; }
    public DateTime? canceledAt { get; set; }
    public string? cancellationReason { get; set; }
    public string checkoutUrl { get; set; } = null!;
    public string qrCode { get; set; } = null!;
}

public class WebhookPayload
{
    public string code { get; set; } = null!;
    public string desc { get; set; } = null!;
    public WebhookData data { get; set; } = null!;
}

public class WebhookData
{
    public string? orderCode { get; set; }
    public int amount { get; set; }
    public string description { get; set; } = null!;
    public string? accountNumber { get; set; }
    public string? reference { get; set; }
    public string? transactionDateTime { get; set; }
    public string? currency { get; set; }
    public string? paymentLinkId { get; set; }
    public string? code { get; set; }
    public string? desc { get; set; }
    public int? counterAccountBankId { get; set; }
    public string? counterAccountBankName { get; set; }
    public string? counterAccountName { get; set; }
    public string? counterAccountNumber { get; set; }
    public string? virtualAccountName { get; set; }
    public string? virtualAccountNumber { get; set; }
}

public class CreatePaymentResult
{
    public PaymentDataDto? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => Data != null;
}

