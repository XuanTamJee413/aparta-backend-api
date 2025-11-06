using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using System.Security.Claims;

namespace ApartaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public BillingController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost("generate-invoices")]
    [Authorize(Policy = "CanGenerateBilling")] 
    public async Task<ActionResult<ApiResponse<object>>> GenerateInvoices([FromBody] GenerateInvoicesRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Request body không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(request.BuildingId))
            {
                return BadRequest(ApiResponse<object>.Fail("BuildingId không được để trống."));
            }

            // Lấy userId từ token (người đăng nhập)
            var userId = User.FindFirst("id")?.Value ?? 
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("User ID not found in token. Please login again."));
            }

            var result = await _invoiceService.GenerateInvoicesAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message));
            }

            return Ok(ApiResponse<object>.Success(
                new { ProcessedCount = result.ProcessedCount },
                result.Message
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"Error: {ex.Message}"));
        }
    }
}

