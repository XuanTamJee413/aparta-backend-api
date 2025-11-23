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
    private readonly ICloudinaryService _cloudinaryService;

    public InvoiceController(IInvoiceService invoiceService, ICloudinaryService cloudinaryService)
    {
        _invoiceService = invoiceService;
        _cloudinaryService = cloudinaryService;
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
        [FromQuery] string? apartmentCode = null,
        [FromQuery] string? feeType = null)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<ApartmentInvoicesDto>>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Note: GetInvoicesGroupedByApartmentAsync doesn't support feeType yet
            // For now, we'll filter after getting results, or update the method
            var groupedInvoices = await _invoiceService.GetInvoicesGroupedByApartmentAsync(buildingId, userId, status, apartmentCode);
            
            // Filter by feeType if provided
            if (!string.IsNullOrWhiteSpace(feeType))
            {
                foreach (var group in groupedInvoices)
                {
                    group.Invoices = group.Invoices.Where(i => i.FeeType == feeType).ToList();
                }
                groupedInvoices = groupedInvoices.Where(g => g.Invoices.Any()).ToList();
            }

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

    // Tạo hóa đơn một lần (One-Time Invoice)
    [HttpPost("one-time")]
    [Authorize(Policy = "CanCreateInvoicePayment")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateOneTimeInvoice([FromForm] OneTimeInvoiceCreateDto dto)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<InvoiceDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            // Upload images if provided
            List<string>? imageUrls = null;
            if (dto.Images != null && dto.Images.Count > 0)
            {
                imageUrls = new List<string>();
                foreach (var image in dto.Images)
                {
                    if (image != null && image.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _cloudinaryService.UploadImageAsync(image, "invoices/evidence");
                            if (!string.IsNullOrEmpty(uploadResult.SecureUrl))
                            {
                                imageUrls.Add(uploadResult.SecureUrl);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue - don't fail the whole request if one image fails
                            // You might want to handle this differently
                        }
                    }
                }
            }

            var result = await _invoiceService.CreateOneTimeInvoiceAsync(dto, userId, imageUrls);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<InvoiceDto>.Fail(result.Message));
            }

            return Ok(ApiResponse<InvoiceDto>.Success(result.Invoice!, result.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<InvoiceDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }

    // Đánh dấu hóa đơn đã thanh toán
    [HttpPut("{id}/mark-paid")]
    [Authorize(Policy = "CanReadInvoiceItem")]
    public async Task<ActionResult<ApiResponse<object>>> MarkInvoiceAsPaid(string id)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var (success, message) = await _invoiceService.MarkInvoiceAsPaidAsync(id, userId);

            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<object>.Success(null, message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }

    // Xóa hóa đơn (chỉ được xóa nếu Status == "PENDING")
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanReadInvoiceItem")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteInvoice(string id)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var (success, message) = await _invoiceService.DeleteInvoiceAsync(id, userId);

            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<object>.Success(null, message));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }

}

