using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CloudinaryController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public CloudinaryController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<CloudinaryUploadResultDto>>> UploadImage([FromForm] CloudinaryUploadRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || request.File == null)
            {
                return BadRequest(ApiResponse<CloudinaryUploadResultDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            try
            {
                var result = await _cloudinaryService.UploadImageAsync(request.File, request.Folder, cancellationToken);
                return Ok(ApiResponse<CloudinaryUploadResultDto>.Success(result, "Tải ảnh lên thành công."));
            }
            catch (ArgumentException)
            {
                return BadRequest(ApiResponse<CloudinaryUploadResultDto>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }
            catch (InvalidOperationException)
            {
                return StatusCode(500, ApiResponse<CloudinaryUploadResultDto>.Fail(ApiResponse.SM40_SYSTEM_ERROR));
            }
        }
    }
}

