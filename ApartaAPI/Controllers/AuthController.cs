using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartaAPI.DTOs.Auth;
using ApartaAPI.Services;
using System.Security.Claims;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.Data;
using Microsoft.EntityFrameworkCore;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApartaDbContext _context;
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            ApartaDbContext context,
            IMailService mailService,
            IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _mailService = mailService;
            _configuration = configuration;
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

        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(ApiResponse.Fail("Email là bắt buộc."));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

            // Trả về Ok ngay cả khi không tìm thấy user để tránh dò user
            if (user == null)
            {
                return Ok(ApiResponse.Success("Nếu email tồn tại, bạn sẽ nhận được link đặt lại mật khẩu."));
            }

            // Tạo token reset
            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // Tạo link reset password
            var frontendUrl = _configuration["Environment:FrontendUrl"] ?? "http://localhost:4200";
            var resetLink = $"{frontendUrl}/reset-password?token={token}&email={Uri.EscapeDataString(request.Email)}";

            // Tạo nội dung email HTML
            var htmlMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #4f46e5;'>Đặt lại mật khẩu</h2>
                        <p>Xin chào,</p>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
                        <p>Vui lòng click vào link bên dưới để đặt lại mật khẩu:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{resetLink}' 
                               style='background-color: #4f46e5; color: white; padding: 12px 24px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Đặt lại mật khẩu
                            </a>
                        </p>
                        <p>Hoặc copy và paste link sau vào trình duyệt:</p>
                        <p style='word-break: break-all; color: #6b7280;'>{resetLink}</p>
                        <p><strong>Lưu ý:</strong> Link này sẽ hết hạn sau 1 giờ.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
                        <p style='color: #6b7280; font-size: 12px;'>
                            Email này được gửi tự động, vui lòng không trả lời.
                        </p>
                    </div>
                </body>
                </html>";

            try
            {
                await _mailService.SendEmailAsync(
                    request.Email,
                    "Đặt lại mật khẩu - Aparta System",
                    htmlMessage
                );

                return Ok(ApiResponse.Success("Nếu email tồn tại, bạn sẽ nhận được link đặt lại mật khẩu."));
            }
            catch (Exception ex)
            {
                // Log error nhưng vẫn trả về success để bảo mật
                // Trong production, nên log vào file hoặc logging service
                return Ok(ApiResponse.Success("Nếu email tồn tại, bạn sẽ nhận được link đặt lại mật khẩu."));
            }
        }

        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(ApiResponse.Fail("Token, Email và mật khẩu mới là bắt buộc."));
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(ApiResponse.Fail("Mật khẩu xác nhận không khớp."));
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(ApiResponse.Fail("Mật khẩu phải có ít nhất 6 ký tự."));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

            if (user == null)
            {
                return BadRequest(ApiResponse.Fail("Email không hợp lệ."));
            }

            // Kiểm tra token
            if (string.IsNullOrWhiteSpace(user.PasswordResetToken) || 
                user.PasswordResetToken != request.Token)
            {
                return BadRequest(ApiResponse.Fail("Token không hợp lệ."));
            }

            // Kiểm tra token hết hạn
            if (user.ResetTokenExpires == null || user.ResetTokenExpires < DateTime.UtcNow)
            {
                return BadRequest(ApiResponse.Fail("Token đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới."));
            }

            // Hash mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Success("Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới."));
        }
    }
}
