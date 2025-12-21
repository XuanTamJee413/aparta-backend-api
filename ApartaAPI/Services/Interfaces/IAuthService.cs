using ApartaAPI.DTOs.Auth;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<User?> GetUserByPhoneAsync(string phone);
        Task<User?> UpdateProfileAsync(string userId, ProfileUpdateDto dto);
    }
}
