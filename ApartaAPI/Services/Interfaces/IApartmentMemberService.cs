using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IApartmentMemberService
    {
        Task<ApiResponse<IEnumerable<ApartmentMemberDto>>> GetAllAsync(ApartmentMemberQueryParameters query);
        Task<ApiResponse<IEnumerable<ApartmentMemberDto>>> GetByUserBuildingsAsync(string userId, ApartmentMemberQueryParameters query);

        Task<ApartmentMemberDto?> GetByIdAsync(string id);
        Task<ApartmentMemberDto> CreateAsync(ApartmentMemberCreateDto dto,IFormFile? faceImageFile = null,CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, ApartmentMemberUpdateDto dto);
        Task<ApiResponse<string>> UpdateFaceImageAsync(string memberId, IFormFile file, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string id);
    }
}
