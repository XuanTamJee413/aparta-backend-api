using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;

namespace ApartaAPI.Services.Interfaces
{
    public interface IApartmentMemberService
    {
        Task<ApiResponse<IEnumerable<ApartmentMemberDto>>> GetAllAsync(ApartmentMemberQueryParameters query);
        Task<ApartmentMemberDto?> GetByIdAsync(string id);
        Task<ApartmentMemberDto> CreateAsync(ApartmentMemberCreateDto dto);
        Task<bool> UpdateAsync(string id, ApartmentMemberUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
