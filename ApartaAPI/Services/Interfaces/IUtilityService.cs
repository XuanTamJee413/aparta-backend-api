
using ApartaAPI.DTOs.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApartaAPI.DTOs.Common; 

namespace ApartaAPI.Services.Interfaces
{
	public interface IUtilityService
	{
		Task<PagedList<UtilityDto>> GetAllUtilitiesAsync(ServiceQueryParameters parameters);
		Task<UtilityDto?> GetUtilityByIdAsync(string id);
		Task<UtilityDto> AddUtilityAsync(UtilityCreateDto utilityDto);
		Task<UtilityDto?> UpdateUtilityAsync(string id, UtilityUpdateDto utilityDto);
		Task<bool> DeleteUtilityAsync(string id);
	}
}