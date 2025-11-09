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
                return BadRequest(ApiResponse<object>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            if (string.IsNullOrWhiteSpace(request.BuildingId))
            {
                return BadRequest(ApiResponse<object>.Fail(ApiResponse.SM02_REQUIRED));
            }

            // Lấy userId từ token (người đăng nhập)
            var userId = User.FindFirst("id")?.Value ?? 
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.Fail(ApiResponse.SM29_USER_NOT_FOUND));
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
        }
    }
}

