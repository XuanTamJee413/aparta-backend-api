using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize] // Tắt authorize tạm thời
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoiceController(IInvoiceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get invoices for the current user
    /// </summary>
    [HttpGet("my-invoices")]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetMyInvoices()
    {
        try
        {
            // Tạm thời dùng user_id cố định vì chưa có authorize
            const string TEMP_USER_ID = "439150F2-BCD4-48F6-B345-047B143D5620";

            var invoices = await _service.GetUserInvoicesAsync(TEMP_USER_ID);

            return Ok(ApiResponse<List<InvoiceDto>>.Success(
                invoices,
                "Lấy danh sách hóa đơn thành công"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<InvoiceDto>>.Fail($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all invoices (for staff)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetInvoices(
        [FromQuery] string? status = null,
        [FromQuery] string? apartmentCode = null)
    {
        try
        {
            var invoices = await _service.GetInvoicesAsync(status, apartmentCode);

            return Ok(ApiResponse<List<InvoiceDto>>.Success(
                invoices,
                "Lấy danh sách hóa đơn thành công"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<InvoiceDto>>.Fail($"Error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Create payment link for an invoice
    /// Resident clicks "Thanh toán" button → This endpoint creates PayOS payment link
    /// </summary>
    [HttpPost("{invoiceId}/pay")]
    public async Task<ActionResult<ApiResponse<string>>> CreatePayment(string invoiceId)
    {
        try
        {
            // Get base URL from request for callback URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            // Create payment link via PayOS
            var checkoutUrl = await _service.CreatePaymentLinkAsync(invoiceId, baseUrl);
            
            if (string.IsNullOrEmpty(checkoutUrl))
            {
                return BadRequest(ApiResponse<string>.Fail(
                    "Không thể tạo link thanh toán. Vui lòng kiểm tra lại hóa đơn."
                ));
            }

            return Ok(ApiResponse<string>.Success(
                checkoutUrl,
                "Tạo link thanh toán thành công"
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error: {ex.Message}"));
        }
    }
}

