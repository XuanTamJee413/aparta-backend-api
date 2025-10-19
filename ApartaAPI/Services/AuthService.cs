using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ApartaAPI.Services
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string phone, string password);
        Task<User?> GetUserByPhoneAsync(string phone);
        Task<IEnumerable<object>> GetRolesAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IRepository<User> userRepository, IRepository<Role> roleRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(string phone, string password)
        {
            var user = await GetUserByPhoneAsync(phone);
            if (user == null) return null;

            // Verify password (assuming password is hashed)
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Get role information
            var role = await _roleRepository.FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
            if (role == null) return null;

            // Generate JWT token
            return GenerateJwtToken(user, role.RoleName ?? "unknown");
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _userRepository.FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted);
        }
        public async Task<IEnumerable<object>> GetRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(r => new { 
                RoleId = r.RoleId, 
                RoleName = r.RoleName 
            });
        }

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "14da3d232e7472b1197c6262937d1aaa49cdc1acc71db952e6aed7f40df50ad6";
            var issuer = jwtSettings["Issuer"] ?? "ApartaAPI";
            var audience = jwtSettings["Audience"] ?? "ApartaAPI";
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
                {
                    new Claim("id", user.UserId),
                    new Claim("name", user.Name),
                    new Claim("phone", user.Phone),
                    new Claim("email", user.Email),
                    new Claim("role", roleName),
                    new Claim("role_id", user.RoleId),
                    new Claim("apartment_id", user.ApartmentId ?? ""),
                    new Claim("staff_code", user.StaffCode ?? "")
                };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
