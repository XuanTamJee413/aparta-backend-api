using ApartaAPI.Data;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Profile;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApartaDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public ProfileService(
            ApartaDbContext context,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<UserProfileDto>> GetUserProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ApiResponse<UserProfileDto>.Fail("UserId không được để trống.");
            }

            // Lấy User với các thông tin liên quan
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Apartment)
                    .ThenInclude(a => a!.Building)
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse<UserProfileDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            // Map sang DTO
            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.Name,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.Phone ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role?.RoleName ?? string.Empty
            };

            // Xử lý thông tin thêm theo Role
            var roleName = user.Role?.RoleName?.ToLower() ?? string.Empty;

            if (roleName == "resident" && user.Apartment != null)
            {
                var buildingName = user.Apartment.Building?.Name ?? "N/A";
                var apartmentCode = user.Apartment.Code ?? "N/A";
                profileDto.ApartmentInfo = $"{apartmentCode} - {buildingName}";
            }
            // [SỬA] Logic lấy thông tin công tác cho Manager/Staff
            else if (roleName != "admin")
            {
                // Lấy danh sách phân công đang ACTIVE
                var activeAssignments = user.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .ToList();

                if (activeAssignments.Any())
                {
                    // 1. Map vào list chi tiết (Mới)
                    profileDto.CurrentAssignments = activeAssignments.Select(sba => new UserAssignmentProfileDto
                    {
                        BuildingId = sba.BuildingId,
                        BuildingName = sba.Building?.Name ?? "Unknown",
                        Position = sba.Position ?? "Nhân viên",
                        ScopeOfWork = sba.ScopeOfWork,
                        StartDate = sba.AssignmentStartDate
                    }).ToList();

                    // 2. Map vào list tên tòa nhà (Cũ - để tương thích)
                    profileDto.ManagedBuildingNames = activeAssignments
                        .Select(sba => sba.Building?.Name ?? "")
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();
                }
            }

            return ApiResponse<UserProfileDto>.Success(profileDto);
        }

        public async Task<ApiResponse<string>> UpdateAvatarAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<string>.Fail(ApiResponse.SM25_INVALID_INPUT);
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return ApiResponse<string>.Fail("UserId không được để trống.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse<string>.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            try
            {
                // Upload ảnh lên Cloudinary
                var uploadResult = await _cloudinaryService.UploadImageAsync(file);

                if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
                {
                    return ApiResponse<string>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
                }

                // (Tùy chọn) Xóa ảnh cũ nếu có PublicId
                // Note: Nếu bạn muốn lưu PublicId để xóa sau, cần thêm field AvatarPublicId vào User model
                // if (!string.IsNullOrWhiteSpace(user.AvatarPublicId))
                // {
                //     await _cloudinaryService.DeletePhotoAsync(user.AvatarPublicId);
                // }

                // Cập nhật AvatarUrl
                user.AvatarUrl = uploadResult.SecureUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ApiResponse<string>.Success(uploadResult.SecureUrl, "Cập nhật ảnh đại diện thành công.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail($"Lỗi khi upload ảnh: {ex.Message}");
            }
        }

        public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ApiResponse.Fail("UserId không được để trống.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            // Verify mật khẩu hiện tại
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse.Fail("Mật khẩu hiện tại không đúng.");
            }

            // Hash mật khẩu mới
            string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // Cập nhật mật khẩu
            user.PasswordHash = newPasswordHash;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse.Success("Đổi mật khẩu thành công.");
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return ApiResponse<UserProfileDto>.Fail("UserId không được để trống.");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Apartment)
                    .ThenInclude(a => a!.Building)
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse<UserProfileDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            // Kiểm tra email trùng (nếu thay đổi)
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserId != userId && !u.IsDeleted);
                if (existingEmail != null)
                {
                    return ApiResponse<UserProfileDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Email");
                }
            }

            // Kiểm tra số điện thoại trùng (nếu thay đổi)
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.Phone)
            {
                var existingPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.Phone == dto.PhoneNumber && u.UserId != userId && !u.IsDeleted);
                if (existingPhone != null)
                {
                    return ApiResponse<UserProfileDto>.Fail(ApiResponse.SM16_DUPLICATE_CODE, "Phone");
                }
            }

            // Cập nhật thông tin
            user.Name = dto.FullName;
            user.Email = dto.Email;
            user.Phone = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Lấy lại thông tin đầy đủ để trả về
            var updatedUser = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Apartment)
                    .ThenInclude(a => a!.Building)
                .Include(u => u.StaffBuildingAssignmentUsers)
                    .ThenInclude(sba => sba.Building)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (updatedUser == null)
            {
                return ApiResponse<UserProfileDto>.Fail(ApiResponse.SM29_USER_NOT_FOUND);
            }

            // Map sang DTO
            var profileDto = new UserProfileDto
            {
                UserId = updatedUser.UserId,
                FullName = updatedUser.Name,
                Email = updatedUser.Email ?? string.Empty,
                PhoneNumber = updatedUser.Phone ?? string.Empty,
                AvatarUrl = updatedUser.AvatarUrl,
                Role = updatedUser.Role?.RoleName ?? string.Empty
            };

            // Xử lý thông tin thêm theo Role
            var roleName = updatedUser.Role?.RoleName?.ToLower() ?? string.Empty;

            if (roleName == "resident" && updatedUser.Apartment != null)
            {
                var buildingName = updatedUser.Apartment.Building?.Name ?? "N/A";
                var apartmentCode = updatedUser.Apartment.Code ?? "N/A";
                profileDto.ApartmentInfo = $"{apartmentCode} - {buildingName}";
            }
            else if (roleName == "manager" || roleName.Contains("staff"))
            {
                profileDto.ManagedBuildingNames = updatedUser.StaffBuildingAssignmentUsers
                    .Where(sba => sba.IsActive)
                    .Select(sba => sba.Building?.Name ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();
            }

            return ApiResponse<UserProfileDto>.Success(profileDto, "Cập nhật thông tin thành công.");
        }
    }
}

