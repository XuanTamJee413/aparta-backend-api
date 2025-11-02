using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using System.Security.Claims;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoiceController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpGet("my-invoices")]
    [Authorize(Policy = "CanReadInvoiceResident")]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetMyInvoices()
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ?? 
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<InvoiceDto>>.Fail("User ID not found in token. Please login again."));
            }

            var invoices = await _service.GetUserInvoicesAsync(userId);

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

    [HttpGet]
    [Authorize(Policy = "CanReadInvoiceStaff")]
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

    [HttpPost("{invoiceId}/pay")]
    [Authorize(Policy = "CanCreateInvoicePayment")]
    public async Task<ActionResult<ApiResponse<string>>> CreatePayment(string invoiceId)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
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

