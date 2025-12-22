using ApartaAPI.Data;
using ApartaAPI.DTOs.Auth;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApartaAPI.Services
{

    public class AuthService : IAuthService
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ApartmentMember> _memberRepository;
        private readonly ApartaDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(IRepository<User> userRepository, IRepository<ApartmentMember> memberRepository, ApartaDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _userRepository = userRepository;
            _memberRepository = memberRepository;
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            // 1. Tìm User
            var user = await _userRepository.FirstOrDefaultAsync(u => u.Phone == request.Phone && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return ApiResponse<LoginResponse>.Fail(ApiResponse.SM07_LOGIN_FAIL);

            if (user.Status?.ToLower() != "active")
                return ApiResponse<LoginResponse>.Fail(ApiResponse.SM17_PERMISSION_DENIED, null, "Tài khoản bị khóa hoặc chưa kích hoạt.");

            // 2. Lấy Role Hệ thống (Resident/Staff)
            var systemRole = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

            var roleName = systemRole?.RoleName ?? "";

            // 3. XỬ LÝ NGỮ CẢNH (Context Resolution)
            ApartmentMember? currentContext = null;
            List<string> permissions = new List<string>();
            string activeRoleDisplay = roleName;

            // Nếu là Cư Dân -> Phải tìm xem đang ở căn nào
            if (roleName == "resident")
            {
                // 3.1 Tìm tất cả căn hộ mà user đang cư trú và có quyền truy cập app
                var memberships = await _memberRepository.FindAsync(m =>
                    m.UserId == user.UserId
                    && m.IsAppAccess // Điều kiện tiên quyết: Phải được cấp quyền App
                    && (
                        m.Status == "Đang cư trú" // Trường hợp 1: Đang ở thực tế
                        ||
                        (m.IsOwner == true && m.Status == "Đi vắng") // Trường hợp 2: Chủ hộ nhưng đang đi vắng/cho thuê
                    )
                );

                if (!memberships.Any())
                    return ApiResponse<LoginResponse>.Fail(ApiResponse.SM13_ACCOUNT_NOT_FOUND, null, "Tài khoản chưa được gán vào căn hộ ghi nhận hoặc không có quyền truy cập.");

                // 3.2 Chọn căn hộ: Ưu tiên căn mặc định -> Hoặc căn đầu tiên
                var selectedMemberId = memberships.FirstOrDefault(m => m.ApartmentId == user.ApartmentId)?.ApartmentMemberId
                                       ?? memberships.First().ApartmentMemberId;

                // 3.3 [SỬA LẠI] Query sâu để lấy thông tin Project/Building validate
                currentContext = await _context.ApartmentMembers
                    .Include(m => m.Apartment)
                        .ThenInclude(a => a.Building)   // [THÊM] Lấy tòa nhà
                        .ThenInclude(b => b.Project)    // [THÊM] Lấy dự án
                    .Include(m => m.Role)
                        .ThenInclude(r => r.Permissions)
                    .FirstOrDefaultAsync(m => m.ApartmentMemberId == selectedMemberId);

                if (currentContext == null)
                    return ApiResponse<LoginResponse>.Fail(ApiResponse.SM13_ACCOUNT_NOT_FOUND);

                // 3.4 [THÊM] VALIDATE TRẠNG THÁI DỰ ÁN & TÒA NHÀ
                var project = currentContext.Apartment?.Building?.Project;
                var building = currentContext.Apartment?.Building;

                if (project == null || !project.IsActive)
                {
                    return ApiResponse<LoginResponse>.Fail(ApiResponse.SM28_PROJECT_NOT_ACTIVE, null, "Dự án này hiện đang tạm ngưng hoạt động.");
                }

                if (building == null || !building.IsActive)
                {
                    return ApiResponse<LoginResponse>.Fail(ApiResponse.SM26_BUILDING_NOT_ACTIVE, null, "Tòa nhà này hiện đang tạm ngưng hoạt động.");
                }

                // 3.5 Cập nhật lại ApartmentId mặc định nếu khác
                if (user.ApartmentId != currentContext.ApartmentId)
                {
                    user.ApartmentId = currentContext.ApartmentId;
                    await _userRepository.UpdateAsync(user);
                }

                // 3.6 Lấy quyền từ Role ngữ cảnh
                activeRoleDisplay = currentContext.Role?.RoleName ?? "resident";
                if (currentContext.Role?.Permissions != null)
                {
                    permissions = currentContext.Role.Permissions.Select(p => p.Name).ToList();
                }
            }
            else
            {
                // Nếu là Staff/Admin -> Lấy quyền từ Role hệ thống
                if (systemRole?.Permissions != null)
                {
                    permissions = systemRole.Permissions.Select(p => p.Name).ToList();
                }
            }

            // 4. Tạo Token
            var token = GenerateJwtToken(user, roleName, currentContext, permissions);

            // 5. Trả về Response
            var response = new LoginResponse(
                Token: token,
                IsFirstLogin: user.IsFirstLogin
            );

            return ApiResponse<LoginResponse>.Success(response, "Đăng nhập thành công.");
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _userRepository.FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted && u.Status.ToLower() == "active");
        }

        private string GenerateJwtToken(User user, string systemRole, ApartmentMember? context, List<string> permissions)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "14da3d232e7472b1197c6262937d1aaa49cdc1acc71db952e6aed7f40df50ad6";
            var issuer = jwtSettings["Issuer"] ?? "ApartaAPI";
            var audience = jwtSettings["Audience"] ?? "ApartaAPI";
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("id", user.UserId),
                new Claim("name", user.Name),
                new Claim("phone", user.Phone ?? ""),
                new Claim("email", user.Email ?? ""),
                new Claim("system_role", systemRole), // Role gốc
                new Claim("staff_code", user.StaffCode ?? "")
            };

            // Thêm Claims ngữ cảnh
            if (context != null)
            {
                claims.Add(new Claim("apartment_id", context.ApartmentId));
                claims.Add(new Claim("member_id", context.ApartmentMemberId));
                claims.Add(new Claim("role", context.Role?.RoleName ?? "unknown")); // Role ngữ cảnh
            }
            else
            {
                claims.Add(new Claim("role", systemRole));
            }

            // Thêm Permissions
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("permission", perm));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> UpdateProfileAsync(string userId, ProfileUpdateDto dto)
        {
            var user = await _userRepository.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return null; 
            }

            _mapper.Map(dto, user);

            user.PasswordHash = user.PasswordHash;
            user.RoleId = user.RoleId;
            user.Status = user.Status;
            user.Phone = user.Phone;   
            user.Email = user.Email;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var success = await _userRepository.SaveChangesAsync();

            return success ? user : null;
        }
    }
}
