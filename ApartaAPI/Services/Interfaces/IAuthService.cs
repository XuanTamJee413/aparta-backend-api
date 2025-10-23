using ApartaAPI.DTOs.Auth;
using ApartaAPI.Models;

namespace ApartaAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string phone, string password);
        Task<User?> GetUserByPhoneAsync(string phone);
        Task<IEnumerable<object>> GetRolesAsync();
        Task<User?> UpdateProfileAsync(string userId, ProfileUpdateDto dto);
    }
}
