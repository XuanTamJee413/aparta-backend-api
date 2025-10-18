using ApartaAPI.DTOs.ApartmentMembers;

namespace ApartaAPI.Services.Interfaces
{
    public interface IApartmentMemberService
    {
        Task<IEnumerable<ApartmentMemberDto>> GetAllAsync();
        Task<ApartmentMemberDto?> GetByIdAsync(string id);
        Task<ApartmentMemberDto> CreateAsync(ApartmentMemberCreateDto dto);
        Task<bool> UpdateAsync(string id, ApartmentMemberUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
