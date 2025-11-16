using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Invoices;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using System.Security.Claims;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    //lấy danh sách hóa đơn của chính mình
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

            var invoices = await _invoiceService.GetUserInvoicesAsync(userId);

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

    // lấy danh sách hóa đơn của tòa nhà, nhóm theo căn hộ
    [HttpGet("/api/buildings/{buildingId}/invoices")]
    [Authorize(Policy = "CanReadInvoiceItem")]
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

            var groupedInvoices = await _invoiceService.GetInvoicesGroupedByApartmentAsync(buildingId, userId, status, apartmentCode);

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

    // lấy chi tiết hóa đơn theo id
    [HttpGet("{invoiceId}")]
    [Authorize(Policy = "CanReadInvoiceItem")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetInvoiceById(string invoiceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return BadRequest(ApiResponse<InvoiceDetailDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<InvoiceDetailDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var invoiceDetail = await _invoiceService.GetInvoiceDetailAsync(invoiceId, userId);

            if (invoiceDetail == null)
            {
                return NotFound(ApiResponse<InvoiceDetailDto>.Fail(ApiResponse.SM01_NO_RESULTS));
            }

            return Ok(ApiResponse<InvoiceDetailDto>.Success(
                invoiceDetail,
                ApiResponse.SM41_INVOICE_DETAIL_SUCCESS
            ));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<InvoiceDetailDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }


}

