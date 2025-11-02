using Microsoft.AspNetCore.Mvc;
using ApartaAPI.Services;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.PayOS;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize] // Tắt authorize tạm thời
public class PayosController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly PayOSService _payOSService;

    public PayosController(IInvoiceService invoiceService, PayOSService payOSService)
    {
        _invoiceService = invoiceService;
        _payOSService = payOSService;
    }

    /// <summary>
    /// Payment success callback (redirect from PayOS)
    /// Automatically updates invoice and payment status when payment is successful
    /// </summary>
    [HttpGet("payment/success")]
    public async Task<ActionResult> PaymentSuccess([FromQuery] string? invoiceId = null, [FromQuery] string? orderCode = null)
    {
        try
        {
            // Check if orderCode is provided
            if (string.IsNullOrEmpty(orderCode))
            {
                return Ok(new
                {
                    success = false,
                    message = "OrderCode không được tìm thấy",
                    invoiceId = invoiceId,
                    orderCode = orderCode
                });
            }

            Console.WriteLine($"Payment success callback: invoiceId={invoiceId}, orderCode={orderCode}");

            // Automatically process payment and update invoice status
            var success = await _invoiceService.ProcessPaymentWebhookAsync(null, orderCode);

            if (!success)
            {
                Console.WriteLine($"Failed to process payment for orderCode={orderCode}");
                return Ok(new
                {
                    success = false,
                    message = "Thanh toán thành công nhưng không thể cập nhật trạng thái. Vui lòng thử lại.",
                    invoiceId = invoiceId,
                    orderCode = orderCode
                });
            }

            Console.WriteLine($"Successfully updated invoice status for orderCode={orderCode}");

            // Return success response
            return Ok(new
            {
                success = true,
                message = "Thanh toán thành công",
                invoiceId = invoiceId,
                orderCode = orderCode
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment success callback error: {ex.Message}");
            return BadRequest(new 
            { 
                success = false, 
                message = ex.Message,
                invoiceId = invoiceId,
                orderCode = orderCode
            });
        }
    }

    /// <summary>
    /// Payment cancel callback (redirect from PayOS)
    /// </summary>
    [HttpGet("payment/cancel")]
    public ActionResult PaymentCancel()
    {
        return Ok(new
        {
            success = false,
            message = "Thanh toán đã bị hủy"
        });
    }

    /// <summary>
    /// Manually trigger payment update (for testing/local development)
    /// Use this endpoint after successful payment when webhook is not available
    /// </summary>
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
            var success = await _invoiceService.ProcessPaymentWebhookAsync(null, request.OrderCode);

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

    /// <summary>
    /// PayOS webhook endpoint - Process payment when PayOS notifies us
    /// </summary>
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
                var success = await _invoiceService.ProcessPaymentWebhookAsync(null, orderCode);
                
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

