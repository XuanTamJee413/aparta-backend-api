using ApartaAPI.Data;
using ApartaAPI.DTOs.Auth;
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
        private readonly ApartaDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(IRepository<User> userRepository, ApartaDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _userRepository = userRepository;
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<string?> LoginAsync(string phone, string password)
        {
            var user = await GetUserByPhoneAsync(phone);
            if (user == null) return null;

            // Verify password (assuming password is hashed)
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Get role information
            var role = await _context.Roles.Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
            if (role == null || !role.IsActive) return null;

            if (role.RoleName == "resident")
            {
                if (string.IsNullOrEmpty(user.ApartmentId))
                {
                    return null;
                }
                var project = await _context.Apartments
                    .Where(a => a.ApartmentId == user.ApartmentId)
                    .Select(a => a.Building.Project)
                    .FirstOrDefaultAsync();

                if (project == null || !project.IsActive)
                {
                    return null;
                }
            }

            // Generate JWT token
            return GenerateJwtToken(user, role);
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _userRepository.FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted && u.Status.ToLower() == "active");
        }

        private string GenerateJwtToken(User user, Role role)
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
                new Claim("phone", user.Phone),
                new Claim("email", user.Email),
                new Claim("role", role.RoleName ?? "unknown"),
                new Claim("role_id", user.RoleId),
                new Claim("apartment_id", user.ApartmentId ?? ""),
                new Claim("staff_code", user.StaffCode ?? "")
            };

            if (role.Permissions != null)
            {
                foreach (var perm in role.Permissions)
                {
                    claims.Add(new Claim("permission", perm.Name));
                }
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
