
using ApartaAPI.DTOs.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartaAPI.DTOs.Common; 

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityService
	{
		Task<PagedList<UtilityDto>> GetAllUtilitiesAsync(ServiceQueryParameters parameters, string userId);
		Task<UtilityDto?> GetUtilityByIdAsync(string id, string userId);
		Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto, string userId);
		Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto, string userId);
		Task<bool> DeleteUtilityAsync(string id, string userId);
		Task<PagedList<UtilityDto>> GetUtilitiesForResidentAsync(ServiceQueryParameters parameters, string residentId);
	}
}