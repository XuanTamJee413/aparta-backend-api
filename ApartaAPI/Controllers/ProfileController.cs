using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Profile;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        // GET: api/profile
        [HttpGet]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.Fail("Không thể xác định người dùng."));
            }

            var response = await _profileService.GetUserProfileAsync(userId);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM29_USER_NOT_FOUND)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/profile/avatar
        [HttpPut("avatar")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateAvatar([FromForm] UpdateAvatarRequest request)
        {
            if (!ModelState.IsValid || request.File == null || request.File.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail(ApiResponse.SM25_INVALID_INPUT));
            }

            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("Không thể xác định người dùng."));
            }

            var response = await _profileService.UpdateAvatarAsync(userId, request.File);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM29_USER_NOT_FOUND)
                {
                    return NotFound(response);
                }
                if (response.Message == ApiResponse.SM40_SYSTEM_ERROR)
                {
                    return StatusCode(500, response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        // POST: api/profile/change-password
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse.Fail(errors));
            }

            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse.Fail("Không thể xác định người dùng."));
            }

            var response = await _profileService.ChangePasswordAsync(userId, dto);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM29_USER_NOT_FOUND)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        // PUT: api/profile - Cập nhật thông tin profile (chỉ dành cho Admin)
        [HttpPut]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<UserProfileDto>.Fail(errors));
            }

            var userId = User.FindFirst("id")?.Value ??
                         User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.Fail("Không thể xác định người dùng."));
            }

            // Kiểm tra role - chỉ admin mới được update profile
            var role = User.FindFirst("role")?.Value ?? 
                       User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrWhiteSpace(role) || !role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, ApiResponse<UserProfileDto>.Fail("Chỉ quản trị viên mới được phép cập nhật thông tin profile."));
            }

            var response = await _profileService.UpdateProfileAsync(userId, dto);

            if (!response.Succeeded)
            {
                if (response.Message == ApiResponse.SM29_USER_NOT_FOUND)
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

