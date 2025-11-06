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
                return Unauthorized(ApiResponse<List<InvoiceDto>>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var invoices = await _service.GetUserInvoicesAsync(userId);

            return Ok(ApiResponse<List<InvoiceDto>>.Success(
                invoices,
                ApiResponse.SM36_INVOICE_LIST_SUCCESS
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<InvoiceDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }

    [HttpGet("/api/buildings/{buildingId}/invoices")]
    [Authorize(Policy = "CanReadInvoiceStaff")]
    public async Task<ActionResult<ApiResponse<List<ApartmentInvoicesDto>>>> GetInvoices(
        [FromRoute] string buildingId,
        [FromQuery] string? status = null,
        [FromQuery] string? apartmentCode = null)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<ApartmentInvoicesDto>>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var groupedInvoices = await _service.GetInvoicesGroupedByApartmentAsync(buildingId, userId, status, apartmentCode);

            return Ok(ApiResponse<List<ApartmentInvoicesDto>>.Success(
                groupedInvoices,
                ApiResponse.SM36_INVOICE_LIST_SUCCESS
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<ApartmentInvoicesDto>>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
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
                return BadRequest(ApiResponse<string>.Fail(ApiResponse.SM39_PAYMENT_LINK_FAILED));
            }

            return Ok(ApiResponse<string>.Success(
                checkoutUrl,
                ApiResponse.SM37_PAYMENT_LINK_SUCCESS
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }
}

