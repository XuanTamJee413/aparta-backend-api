using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Invoices;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using System;
using System.Security.Claims;
using ApartaAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ApartaDbContext _context;

    public InvoiceController(IInvoiceService invoiceService, ICloudinaryService cloudinaryService, ApartaDbContext context)
    {
        _invoiceService = invoiceService;
        _cloudinaryService = cloudinaryService;
        _context = context;
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

    //lấy chi tiết hóa đơn của chính mình (dành cho resident)
    [HttpGet("my-invoices/{invoiceId}")]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetMyInvoiceDetail(string invoiceId)
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

            var invoiceDetail = await _invoiceService.GetResidentInvoiceDetailAsync(invoiceId, userId);

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

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được xem invoice của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == buildingId 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập building này
                }
            }

            // Lấy danh sách hóa đơn theo căn hộ
            var groupedInvoices = await _invoiceService.GetInvoicesGroupedByApartmentAsync(buildingId, userId, status, apartmentCode);
            
            // Lọc theo feeType nếu có
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

            // Kiểm tra quyền: tất cả role (trừ admin) chỉ được tạo invoice cho apartment của building được gán
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = !string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && !string.IsNullOrEmpty(dto.ApartmentId))
            {
                // Lấy buildingId từ apartmentId
                var apartment = await _context.Apartments
                    .Where(a => a.ApartmentId == dto.ApartmentId)
                    .Select(a => a.BuildingId)
                    .FirstOrDefaultAsync();

                if (apartment == null)
                {
                    return NotFound(ApiResponse<InvoiceDto>.Fail(ApiResponse.SM01_NO_RESULTS));
                }

                // Kiểm tra xem user có được gán quản lý building này không
                var hasAccess = await _context.StaffBuildingAssignments
                    .AnyAsync(sba => sba.UserId == userId 
                        && sba.BuildingId == apartment 
                        && sba.IsActive);

                if (!hasAccess)
                {
                    return Forbid(); // 403 - Không có quyền truy cập apartment này
                }
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

    // Cập nhật ngày kết thúc thanh toán cho hóa đơn quá hạn
    [HttpPut("{id}/update-end-date")]
    [Authorize(Policy = "CanReadInvoiceItem")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateInvoiceEndDate(string id, [FromBody] UpdateInvoiceEndDateDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(ApiResponse<object>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
            }

            var (success, message) = await _invoiceService.UpdateInvoiceEndDateAsync(id, dto.EndDate, userId);

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

