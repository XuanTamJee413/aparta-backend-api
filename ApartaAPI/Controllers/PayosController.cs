using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.PayOS;
using ApartaAPI.DTOs.Common;
using System;

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
            // Verify webhook signature to ensure it's from PayOS
            if (!_payOSService.VerifyWebhookSignature(payload, signature))
            {
                return Unauthorized();
            }

            // Check if payment is successful
            // PayOS returns code "00" when payment is successful
            if (payload.data.code == "00")
            {
                // Find invoiceId from Payment record using orderCode
                // We stored orderCode in Payment.PaymentCode when creating payment link
                var orderCode = payload.data.orderCode.ToString();
                
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
}

