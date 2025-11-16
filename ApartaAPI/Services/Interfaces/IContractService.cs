using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;

namespace ApartaAPI.Services.Interfaces
{
    public interface IContractService
    {
        Task<ApiResponse<IEnumerable<ContractDto>>> GetAllAsync(ContractQueryParameters query);
        Task<ContractDto?> GetByIdAsync(string id);
        Task<ContractDto> CreateAsync(ContractCreateDto dto); 
        Task<bool> UpdateAsync(string id, ContractUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
