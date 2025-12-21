using ApartaAPI.DTOs;
using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;

namespace ApartaAPI.Services.Interfaces
{
    public interface IContractService
    {
        Task<ApiResponse<IEnumerable<ContractDto>>> GetAllAsync(ContractQueryParameters query);
        Task<ContractDto?> GetByIdAsync(string id);
        Task<ApiResponse<ContractDto>> CreateAsync(CreateContractRequestDto request);
        Task<bool> UpdateAsync(string id, ContractUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<ApiResponse<IEnumerable<ContractDto>>> GetByUserBuildingsAsync(string userId, ContractQueryParameters query);

    }
}
