using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using ApartaAPI.DTOs.PayOS;

namespace ApartaAPI.Services;

public class PayOSService
{
    private readonly PayOSClient _payOSClient;
    private readonly string _checksumKey;

    public PayOSService(IConfiguration configuration)
    {
        var clientId = configuration["Environment:PAYOS_CLIENT_ID"] 
            ?? throw new Exception("Missing PAYOS_CLIENT_ID");
        var apiKey = configuration["Environment:PAYOS_API_KEY"] 
            ?? throw new Exception("Missing PAYOS_API_KEY");
        _checksumKey = configuration["Environment:PAYOS_CHECKSUM_KEY"] 
            ?? throw new Exception("Missing PAYOS_CHECKSUM_KEY");

        // Initialize PayOS SDK v2.0.1
        _payOSClient = new PayOSClient(clientId, apiKey, _checksumKey);
    }

    // Constructor mới: nhận settings trực tiếp từ project
    public PayOSService(string clientId, string apiKey, string checksumKey)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new Exception("PayOS Client ID is required");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("PayOS API Key is required");
        if (string.IsNullOrWhiteSpace(checksumKey))
            throw new Exception("PayOS Checksum Key is required");

        _checksumKey = checksumKey;
        // Initialize PayOS SDK v2.0.1
        _payOSClient = new PayOSClient(clientId, apiKey, checksumKey);
    }

    // Factory method để tạo instance từ project settings
    public static PayOSService CreateFromProject(string? clientId, string? apiKey, string? checksumKey, IConfiguration? fallbackConfig = null)
    {
        // Ưu tiên sử dụng settings từ project
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(checksumKey))
        {
            return new PayOSService(clientId, apiKey, checksumKey);
        }

        // Fallback về configuration nếu project chưa có settings
        if (fallbackConfig != null)
        {
            return new PayOSService(fallbackConfig);
        }

        throw new Exception("PayOS settings not found. Please configure PayOS settings in Project or appsettings.json");
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(string invoiceId, decimal amount, string description, string cancelUrl, string returnUrl)
    {
        try
        {
            // PayOS expects orderCode to be unique - using timestamp milliseconds
            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // PayOS SDK v2.0.1 - Create payment link request
            Console.WriteLine($"Creating PayOS payment for invoice {invoiceId}, Amount: {amount}");
            
            // PayOS requires description to be max 25 characters
            // Use invoice ID (first 17 chars) to fit within limit, or use provided description if shorter
            string paymentDescription;
            if (!string.IsNullOrEmpty(description) && description.Length <= 25)
            {
                // Use provided description if it's within limit
                paymentDescription = description;
            }
            else
            {
                // Create short description from invoice ID: "HD_{shortId}" (max 25 chars)
                // "HD_" = 3 chars, so we have 22 chars for ID
                var shortInvoiceId = invoiceId.Length > 22 ? invoiceId.Substring(0, 22) : invoiceId;
                paymentDescription = $"HD_{shortInvoiceId}";
                
                // Final safety check: ensure it's exactly 25 chars or less
                if (paymentDescription.Length > 25)
                {
                    paymentDescription = paymentDescription.Substring(0, 25);
                }
            }
            
            Console.WriteLine($"PayOS Payment Description (max 25 chars): {paymentDescription} (length: {paymentDescription.Length})");
            
            // Create payment link request using SDK's CreatePaymentLinkRequest class
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)amount,
                Description = paymentDescription,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()
            };
            
            // Use SDK PaymentRequests.CreateAsync method
            var result = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);
            
            if (result == null)
            {
                var errorMsg = "PayOS Error: Failed to create payment link (result is null)";
                Console.WriteLine(errorMsg);
                return new CreatePaymentResult { ErrorMessage = errorMsg };
            }

            Console.WriteLine($"PayOS Success: Payment link created, checkoutUrl: {result.CheckoutUrl}");
            
            // Convert PayOS SDK result to our PaymentDataDto format
            // Note: CreatePaymentLinkResponse may not have all properties like CreatedAt, Transactions, etc.
            return new CreatePaymentResult 
            { 
                Data = new PaymentDataDto
                {
                    bin = result.Bin ?? "",
                    accountNumber = result.AccountNumber ?? "",
                    accountName = result.AccountName ?? "",
                    amount = result.Amount,
                    description = result.Description ?? "",
                    orderCode = result.OrderCode,
                    currency = result.Currency ?? "VND",
                    paymentLinkId = result.PaymentLinkId ?? "",
                    status = result.Status.ToString(), 
                    createdAt = DateTime.Now, 
                    transactions = null,
                    canceledAt = null, 
                    cancellationReason = null, 
                    checkoutUrl = result.CheckoutUrl ?? "",
                    qrCode = result.QrCode ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message;
            Console.WriteLine($"PayOS Exception: {errorMsg}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            return new CreatePaymentResult { ErrorMessage = errorMsg };
        }
    }


    public bool VerifyWebhookSignature(WebhookPayload payload, string signature)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var keyBytes = Encoding.UTF8.GetBytes(_checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(json);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var calculatedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return calculatedSignature == signature.ToLower();
        }
        catch
        {
            return false;
        }
    }
}

