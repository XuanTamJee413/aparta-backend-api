using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace ApartaAPI.Services.Interfaces
{
    public interface IProfileService
    {
        Task<ApiResponse<UserProfileDto>> GetUserProfileAsync(string userId);
        Task<ApiResponse<string>> UpdateAvatarAsync(string userId, IFormFile file);
        Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    }
}

