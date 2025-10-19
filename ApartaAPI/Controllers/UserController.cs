using ApartaAPI.DTOs.Auth;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace ApartaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase 
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public UserController(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        [HttpPost("update-profile")]
        public async Task<ActionResult<UserInfoResponse>> UpdateProfile([FromBody] ProfileUpdateDto request)
        {
            var userId = User.FindFirst("id")?.Value;

            var roleNameFromToken = User.FindFirst(ClaimTypes.Role)?.Value?.Trim();
            if (string.IsNullOrEmpty(roleNameFromToken))
            {
                roleNameFromToken = User.FindFirst("role")?.Value?.Trim();
            }

            // ------------------------------------------------------------------

            if (userId == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(roleNameFromToken))
            {
                roleNameFromToken = "unknown";
            }

            var updatedUser = await _authService.UpdateProfileAsync(userId, request);

            if (updatedUser == null)
            {
                return NotFound();
            }

            var response = _mapper.Map<UserInfoResponse>(updatedUser);

            response = response with { Role = roleNameFromToken };

            return Ok(response);
        }
    }
}
