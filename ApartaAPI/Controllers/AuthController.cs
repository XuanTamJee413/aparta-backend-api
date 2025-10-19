using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Auth;
using ApartaAPI.Services;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Phone and password are required");
            }

            var token = await _authService.LoginAsync(request.Phone, request.Password);
            if (token == null)
            {
                return Unauthorized("Invalid phone or password");
            }

            var user = await _authService.GetUserByPhoneAsync(request.Phone);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var response = new LoginResponse(
                Token: token
            );

            return Ok(response);
        }

        // GET: api/Auth/me
        [HttpGet("me")]
        [Authorize]
        public ActionResult<UserInfoResponse> GetCurrentUser()
        {
            var userId = User.FindFirst("id")?.Value;
            var name = User.FindFirst("name")?.Value;
            var phone = User.FindFirst("phone")?.Value;
            var email = User.FindFirst("email")?.Value;
            var role = User.FindFirst("role")?.Value;

            var apartmentId = User.FindFirst("apartment_id")?.Value;
            var staffCode = User.FindFirst("staff_code")?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            var response = new UserInfoResponse(
                UserId: userId,
                Name: name ?? "",
                Phone: phone ?? "",
                Email: email ?? "",
                Role: role ?? "unknown",
                ApartmentId: apartmentId,
                StaffCode: staffCode,
                Status: "active", // You might want to get this from database
                LastLoginAt: DateTime.UtcNow
            );

            return Ok(response);
        }

        // GET: api/Auth/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoles()
        {
            var roles = await _authService.GetRolesAsync();
            return Ok(roles);
        }
    }
}
