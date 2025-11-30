using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.PayOS;
using ApartaAPI.DTOs.Common;
using System;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayosController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly PayOSService _payOSService;
    private readonly IConfiguration _configuration;

    public PayosController(IPaymentService paymentService, PayOSService payOSService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _payOSService = payOSService;
        _configuration = configuration;
    }

    // tạo liên kết thanh toán cho hóa đơn
    [HttpPost("payment/create/{invoiceId}")]
    [Authorize(Policy = "CanCreateInvoicePayment")]
    public async Task<ActionResult<ApiResponse<string>>> CreatePayment(string invoiceId)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _paymentService.CreatePaymentLinkAsync(invoiceId, baseUrl);

            if (string.IsNullOrEmpty(checkoutUrl))
            {
                return BadRequest(ApiResponse<string>.Fail(ApiResponse.SM39_PAYMENT_LINK_FAILED));
            }

            return Ok(ApiResponse<string>.Success(
                checkoutUrl,
                ApiResponse.SM37_PAYMENT_LINK_SUCCESS
            ));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<string>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }

    [HttpGet("payment/success")]
    public async Task<ActionResult> PaymentSuccess([FromQuery] string? invoiceId = null, [FromQuery] string? orderCode = null)
    {
        try
        {
            var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
            
            if (string.IsNullOrEmpty(orderCode))
            {
                var errorUrl = $"{frontendUrl}/home?payment=error&message=" + Uri.EscapeDataString("OrderCode không được tìm thấy");
                return Redirect(errorUrl);
            }

            Console.WriteLine($"Payment success callback: invoiceId={invoiceId}, orderCode={orderCode}");

            var success = await _paymentService.ProcessPaymentWebhookAsync(null, orderCode);

            if (!success)
            {
                Console.WriteLine($"Failed to process payment for orderCode={orderCode}");
                var errorUrl = $"{frontendUrl}/home?payment=error&message=" + Uri.EscapeDataString("Thanh toán thành công nhưng không thể cập nhật trạng thái. Vui lòng thử lại.") + $"&invoiceId={invoiceId}&orderCode={orderCode}";
                return Redirect(errorUrl);
            }

            Console.WriteLine($"Successfully updated invoice status for orderCode={orderCode}");

            var redirectUrl = $"{frontendUrl}/home?payment=success&invoiceId={invoiceId}&orderCode={orderCode}";
            
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment success callback error: {ex.Message}");
            var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
            var errorUrl = $"{frontendUrl}/home?payment=error&message=" + Uri.EscapeDataString(ex.Message) + $"&invoiceId={invoiceId}&orderCode={orderCode}";
            return Redirect(errorUrl);
        }
    }

    [HttpGet("payment/cancel")]
    public ActionResult PaymentCancel([FromQuery] string? invoiceId = null)
    {
        var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
        var redirectUrl = $"{frontendUrl}/home?payment=cancel" + (string.IsNullOrEmpty(invoiceId) ? "" : $"&invoiceId={invoiceId}");
        
        return Redirect(redirectUrl);
    }

    [HttpPost("payment/manual-update")]
    public async Task<IActionResult> ManualPaymentUpdate([FromBody] ManualPaymentUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.OrderCode))
            {
                return BadRequest(new { success = false, message = "OrderCode is required" });
            }

            Console.WriteLine($"Manual payment update triggered: orderCode={request.OrderCode}");

            // Process payment update (same logic as webhook)
            var success = await _paymentService.ProcessPaymentWebhookAsync(null, request.OrderCode);

            if (!success)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to update payment. Check if orderCode exists and payment record is found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Payment updated successfully",
                orderCode = request.OrderCode
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Manual payment update error: {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error: {ex.Message}"
            });
        }
    }

    public class ManualPaymentUpdateRequest
    {
        public string OrderCode { get; set; } = null!;
    }

    [HttpPost("payment/webhook")]
    public async Task<IActionResult> PaymentWebhook(
        [FromBody] WebhookPayload payload,
        [FromHeader(Name = "x-payos-signature")] string signature)
    {
        try
        {
            // Tìm payment record để lấy project settings
            var orderCode = payload.data.orderCode ?? string.Empty;
            var payment = await _paymentService.GetPaymentByOrderCodeAsync(orderCode);
            
            PayOSService payOSService = _payOSService; // Default fallback
            
            if (payment != null)
            {
                // Lấy project từ invoice để verify với đúng checksumKey
                var project = await _paymentService.GetProjectFromPaymentAsync(payment.PaymentId);
                if (project != null && !string.IsNullOrWhiteSpace(project.PayOSChecksumKey))
                {
                    // Tạo PayOSService với settings từ project để verify
                    payOSService = PayOSService.CreateFromProject(
                        project.PayOSClientId,
                        project.PayOSApiKey,
                        project.PayOSChecksumKey,
                        _configuration
                    );
                }
            }

            // Verify webhook signature to ensure it's from PayOS
            if (!payOSService.VerifyWebhookSignature(payload, signature))
            {
                return Unauthorized();
            }

            // Check if payment is successful
            // PayOS returns code "00" when payment is successful
            if (payload.data.code == "00")
            {
                
                Console.WriteLine($"Webhook received: orderCode={orderCode}, code={payload.data.code}");
                
                // Process payment and update invoice status
                // ProcessPaymentWebhookAsync will find invoiceId from Payment table using orderCode
                var success = await _paymentService.ProcessPaymentWebhookAsync(null, orderCode);
                
                if (!success)
                {
                    Console.WriteLine($"Failed to process payment webhook for orderCode={orderCode}");
                    return StatusCode(500, "Failed to process payment");
                }
                
                Console.WriteLine($"Successfully processed payment webhook for orderCode={orderCode}");
            }

            // Return 200 OK to acknowledge webhook (important for PayOS)
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    // Validate PayOS credentials
    [HttpPost("validate-credentials")]
    [Authorize(Policy = "CanCreateProject")]
    public async Task<ActionResult<ApiResponse<PayOSValidationResponse>>> ValidatePayOSCredentials([FromBody] PayOSValidationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ClientId) || 
                string.IsNullOrWhiteSpace(request.ApiKey) || 
                string.IsNullOrWhiteSpace(request.ChecksumKey))
            {
                return BadRequest(ApiResponse<PayOSValidationResponse>.Fail(
                    ApiResponse.SM25_INVALID_INPUT,
                    null,
                    "Vui lòng nhập đầy đủ thông tin PayOS"
                ));
            }

            // Tạo PayOSService với credentials được cung cấp
            PayOSService testPayOSService;
            try
            {
                testPayOSService = new PayOSService(request.ClientId, request.ApiKey, request.ChecksumKey);
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<PayOSValidationResponse>.Success(
                    new PayOSValidationResponse(false, $"Lỗi khởi tạo PayOS: {ex.Message}"),
                    "Validation failed"
                ));
            }

            // Thử tạo một payment link test để verify credentials
            // PayOS không có endpoint riêng để verify, nên ta sẽ thử tạo payment với số tiền nhỏ nhất
            try
            {
                var testResult = await testPayOSService.CreatePaymentAsync(
                    "test-validation",
                    1000, // 1000 VND - số tiền tối thiểu
                    "Test validation",
                    "https://example.com/cancel",
                    "https://example.com/return"
                );

                if (testResult.IsSuccess && testResult.Data != null)
                {
                    // Lấy thông tin tài khoản từ PayOS response
                    // Note: PayOS trả về accountNumber và accountName, nhưng không có bankName
                    // BankName có thể cần tra cứu từ BIN hoặc để user tự nhập
                    return Ok(ApiResponse<PayOSValidationResponse>.Success(
                        new PayOSValidationResponse(
                            true, 
                            "Tài khoản PayOS hợp lệ",
                            null, // BankName - không có trong PayOS response, để user tự nhập
                            testResult.Data.accountNumber, // Số tài khoản
                            testResult.Data.accountName // Tên chủ tài khoản
                        ),
                        "Validation successful"
                    ));
                }
                else
                {
                    return Ok(ApiResponse<PayOSValidationResponse>.Success(
                        new PayOSValidationResponse(false, testResult.ErrorMessage ?? "Không thể xác thực tài khoản PayOS"),
                        "Validation failed"
                    ));
                }
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<PayOSValidationResponse>.Success(
                    new PayOSValidationResponse(false, $"Lỗi khi kiểm tra: {ex.Message}"),
                    "Validation failed"
                ));
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PayOSValidationResponse>.Fail(
                ApiResponse.SM40_SYSTEM_ERROR,
                null,
                ex.Message
            ));
        }
    }
}

